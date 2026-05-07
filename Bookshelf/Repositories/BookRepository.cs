using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.ViewModels;

namespace Bookshelf.Repositories;

/// <summary>
/// Ownership-scoped data access for Books. Books inherit ownership through
/// their Author (Author.UserId), so every query filters via the author
/// relationship. The current user's id is captured once at construction time
/// so every method automatically scopes to that user without needing it
/// passed at each call-site.
/// </summary>
public class BookRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _userId;

    public BookRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _context = context;
        _userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("BookRepository requires an authenticated user.");
    }

    public Task<List<Book>> ListAsync()
    {
        return _context.Books
            .Where(b => b.Author.UserId == _userId)
            .Include(b => b.Author)
            .ToListAsync();
    }

    public Task<Book?> FindAsync(int id)
    {
        return _context.Books
            .FirstOrDefaultAsync(b => b.Id == id && b.Author.UserId == _userId);
    }

    public Task<Book?> FindWithAuthorAsync(int id)
    {
        return _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id && b.Author.UserId == _userId);
    }

    /// <summary>
    /// Validates and creates a new Book from the submitted view model.
    /// Ownership of the selected AuthorId is enforced here so a tampered
    /// POST submitting another user's author id is rejected.
    /// </summary>
    public async Task<RepositoryResult> CreateAsync(BookFormViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var validationResult = await ValidateAsync(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var book = new Book();
        ApplyFormData(book, viewModel);
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return RepositoryResult.Success;
    }

    /// <summary>
    /// Validates and updates the Book identified by <paramref name="id"/> from
    /// the submitted view model. Returns NotFound if the book doesn't exist or
    /// isn't owned by the current user.
    /// </summary>
    public async Task<RepositoryResult> UpdateAsync(int id, BookFormViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var book = await FindAsync(id);
        if (book == null)
        {
            return RepositoryResult.NotFound;
        }

        var validationResult = await ValidateAsync(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        ApplyFormData(book, viewModel);
        await _context.SaveChangesAsync();
        return RepositoryResult.Success;
    }

    public void Remove(Book book)
    {
        _context.Books.Remove(book);
    }

    public async Task<SelectList> BuildAuthorsSelectListAsync(int? selectedAuthorId = null)
    {
        var authors = await _context.Authors
            .Where(a => a.UserId == _userId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return new SelectList(authors, "Id", "Name", selectedAuthorId);
    }

    public Task SaveAsync()
    {
        return _context.SaveChangesAsync();
    }

    private async Task<RepositoryResult> ValidateAsync(BookFormViewModel viewModel)
    {
        return await IsOwnedAuthorAsync(viewModel.AuthorId)
            ? RepositoryResult.Success
            : RepositoryResult.ValidationFailed(
                nameof(viewModel.AuthorId),
                "Invalid author selection.");
    }

    private Task<bool> IsOwnedAuthorAsync(int authorId)
    {
        return _context.Authors
            .AnyAsync(a => a.Id == authorId && a.UserId == _userId);
    }

    private static void ApplyFormData(Book book, BookFormViewModel viewModel)
    {
        book.Title = viewModel.Title;
        book.Isbn = viewModel.Isbn;
        book.Year = viewModel.Year;
        book.AuthorId = viewModel.AuthorId;
        book.CoverImagePath = viewModel.CoverImagePath;
    }
}
