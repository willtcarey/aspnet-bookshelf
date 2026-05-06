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

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IFileStorage fileStorage)
        : base(options)
    {
        _fileStorage = fileStorage;
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        // Per-user ownership (issue #1).
        //
        // Authors belong directly to a user. Books inherit ownership through
        // their Author (Author -> User), so Books don't need their own UserId.
        // The existing Author -> Book cascade on FK_Books_Authors_AuthorId means
        // deleting a user cascades: User -> Authors -> Books.
        builder.Entity<Author>(entity =>
        {
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.UserId);
        });
    }

    // Automatically manages file lifecycle for properties marked with [FileAttachment].
    //
    // Uses the change tracker to handle cleanup:
    //   - Added entity: rolls back the uploaded file if SaveChanges fails
    //   - Modified entity: deletes the old file on success, rolls back the new file on failure
    //   - Deleted entity: deletes the file on success
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
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
}
