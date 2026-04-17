using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;

namespace Bookshelf.Repositories;

/// <summary>
/// Ownership-scoped data access for Books. Every method requires a userId
/// parameter, making it structurally impossible to accidentally query or
/// modify another user's data.
/// </summary>
public class BookRepository
{
    private readonly ApplicationDbContext _context;

    public BookRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Book>> ListAsync(string userId)
    {
        return _context.Books
            .Where(b => b.UserId == userId)
            .Include(b => b.Author)
            .ToListAsync();
    }

    public Task<Book?> FindAsync(int id, string userId)
    {
        return _context.Books
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
    }

    public Task<Book?> FindWithAuthorAsync(int id, string userId)
    {
        return _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
    }

    public void Add(Book book, string userId)
    {
        book.UserId = userId;
        _context.Books.Add(book);
    }

    public void Remove(Book book)
    {
        _context.Books.Remove(book);
    }

    public Task<bool> IsOwnedAuthorAsync(int authorId, string userId)
    {
        return _context.Authors
            .AnyAsync(a => a.Id == authorId && a.UserId == userId);
    }

    public async Task<SelectList> BuildAuthorsSelectListAsync(string userId, int? selectedAuthorId = null)
    {
        var authors = await _context.Authors
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return new SelectList(authors, "Id", "Name", selectedAuthorId);
    }

    public Task SaveAsync()
    {
        return _context.SaveChangesAsync();
    }
}
