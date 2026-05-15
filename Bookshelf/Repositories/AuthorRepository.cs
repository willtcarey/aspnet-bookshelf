using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.ViewModels;

namespace Bookshelf.Repositories;

/// <summary>
/// Ownership-scoped data access for Authors. The current user's id is
/// captured once at construction time so every method automatically scopes
/// to that user without needing it passed at each call-site.
/// </summary>
public class AuthorRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _userId;

    public AuthorRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _context = context;
        _userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("AuthorRepository requires an authenticated user.");
    }

    public Task<List<Author>> ListAsync()
    {
        return _context.Authors
            .Where(a => a.UserId == _userId)
            .ToListAsync();
    }

    public Task<Author?> FindAsync(int id)
    {
        return _context.Authors
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == _userId);
    }

    public Task<Author?> FindWithBooksAsync(int id)
    {
        return _context.Authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == _userId);
    }

    /// <summary>
    /// Validates and creates a new Author from the submitted view model. The
    /// new author is automatically scoped to the current user.
    /// </summary>
    public async Task<RepositoryResult> CreateAsync(AuthorFormViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var validationResult = await ValidateAsync(viewModel, currentId: null);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var author = new Author { UserId = _userId };
        ApplyFormData(author, viewModel);
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        return RepositoryResult.Success;
    }

    /// <summary>
    /// Validates and updates the Author identified by <paramref name="id"/> from
    /// the submitted view model. Returns NotFound if the author doesn't exist or
    /// isn't owned by the current user (including the case where another
    /// request deleted it between load and save).
    /// </summary>
    public async Task<RepositoryResult> UpdateAsync(int id, AuthorFormViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var author = await FindAsync(id);
        if (author == null)
        {
            return RepositoryResult.NotFound;
        }

        var validationResult = await ValidateAsync(viewModel, currentId: id);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        ApplyFormData(author, viewModel);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ExistsAsync(id))
            {
                return RepositoryResult.NotFound;
            }

            throw;
        }

        return RepositoryResult.Success;
    }

    public void Remove(Author author)
    {
        _context.Authors.Remove(author);
    }

    public Task SaveAsync()
    {
        return _context.SaveChangesAsync();
    }

    private Task<bool> ExistsAsync(int id)
    {
        return _context.Authors
            .AnyAsync(a => a.Id == id && a.UserId == _userId);
    }

    private async Task<RepositoryResult> ValidateAsync(AuthorFormViewModel viewModel, int? currentId)
    {
        return await NameExistsForUserAsync(viewModel.Name, currentId)
            ? RepositoryResult.ValidationFailed(
                nameof(viewModel.Name),
                "You already have an author with this name.")
            : RepositoryResult.Success;
    }

    private Task<bool> NameExistsForUserAsync(string name, int? excludingId)
    {
        return _context.Authors
            .AnyAsync(a => a.UserId == _userId && a.Name == name && (excludingId == null || a.Id != excludingId));
    }

    private static void ApplyFormData(Author author, AuthorFormViewModel viewModel)
    {
        author.Name = viewModel.Name;
    }
}
