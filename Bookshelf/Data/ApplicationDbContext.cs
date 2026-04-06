using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Attributes;
using Bookshelf.Models;
using Bookshelf.Services;

namespace Bookshelf.Data;

// We inherit from IdentityDbContext to keep app models and identity in a single context.
// These could be separated into multiple contexts, but we'd lose the ability to create
// navigation properties between our models and IdentityUser. Separate contexts would be
// preferred if our users aren't database-backed in the same way (e.g., external auth).
public class ApplicationDbContext : IdentityDbContext
{
    private readonly IFileStorage _fileStorage;
    private readonly List<PendingFileAttachment> _pendingAttachments = new();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IFileStorage fileStorage)
        : base(options)
    {
        _fileStorage = fileStorage;
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();

    // Queue a file to be saved to storage when SaveChangesAsync is called.
    // The resulting storage path will be set on the entity's property automatically.
    // Pass null or an empty file to no-op (safe to call unconditionally with form data).
    public void AttachFile(object entity, string propertyName, IFormFile? file)
    {
        if (file is not { Length: > 0 }) return;

        _pendingAttachments.Add(new PendingFileAttachment(entity, propertyName, file));
    }

    // Automatically manages file lifecycle for properties marked with [FileAttachment].
    //
    // Before saving:
    //   - Processes pending file attachments queued via AttachFile(), saving them to
    //     storage and setting the resulting path on the entity property
    //
    // After saving, uses the change tracker to handle cleanup:
    //   - Added entity: rolls back the uploaded file if SaveChanges fails
    //   - Modified entity: deletes the old file on success, rolls back the new file on failure
    //   - Deleted entity: deletes the file on success
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ProcessPendingAttachmentsAsync();

        var pathsToDeleteOnSuccess = new List<string>();
        var pathsToDeleteOnFailure = new List<string>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var attachmentProperties = entry.Entity.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<FileAttachmentAttribute>() != null);

            foreach (var prop in attachmentProperties)
            {
                var propertyName = prop.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                    {
                        var currentPath = entry.CurrentValues.GetValue<string?>(propertyName);
                        if (!string.IsNullOrWhiteSpace(currentPath))
                            pathsToDeleteOnFailure.Add(currentPath);
                        break;
                    }

                    case EntityState.Modified:
                    {
                        var currentPath = entry.CurrentValues.GetValue<string?>(propertyName);
                        var originalPath = entry.OriginalValues.GetValue<string?>(propertyName);

                        if (originalPath != currentPath)
                        {
                            if (!string.IsNullOrWhiteSpace(originalPath))
                                pathsToDeleteOnSuccess.Add(originalPath);
                            if (!string.IsNullOrWhiteSpace(currentPath))
                                pathsToDeleteOnFailure.Add(currentPath);
                        }
                        break;
                    }

                    case EntityState.Deleted:
                    {
                        var currentPath = entry.CurrentValues.GetValue<string?>(propertyName);
                        if (!string.IsNullOrWhiteSpace(currentPath))
                            pathsToDeleteOnSuccess.Add(currentPath);
                        break;
                    }
                }
            }
        }

        int result;
        try
        {
            result = await base.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var path in pathsToDeleteOnFailure)
                await _fileStorage.DeleteAsync(path);
            throw;
        }

        foreach (var path in pathsToDeleteOnSuccess)
            await _fileStorage.DeleteAsync(path);

        return result;
    }

    private async Task ProcessPendingAttachmentsAsync()
    {
        foreach (var attachment in _pendingAttachments)
        {
            await using var stream = attachment.File.OpenReadStream();
            var path = await _fileStorage.SaveAsync(
                stream, attachment.File.FileName, attachment.File.ContentType);

            var property = attachment.Entity.GetType().GetProperty(attachment.PropertyName);
            property!.SetValue(attachment.Entity, path);
        }

        _pendingAttachments.Clear();
    }

    private record PendingFileAttachment(object Entity, string PropertyName, IFormFile File);
}
