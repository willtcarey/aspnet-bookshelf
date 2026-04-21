using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;

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

    public void Add(Book book)
    {
        _context.Books.Add(book);
    }

    public void Remove(Book book)
    {
        _context.Books.Remove(book);
    }

    public Task<bool> IsOwnedAuthorAsync(int authorId)
    {
        return _context.Authors
            .AnyAsync(a => a.Id == authorId && a.UserId == _userId);
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
}
