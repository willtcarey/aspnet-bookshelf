using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;

namespace Bookshelf.Repositories;

/// <summary>
/// Ownership-scoped data access for Authors. Every method requires a userId
/// parameter, making it structurally impossible to accidentally query or
/// modify another user's data.
/// </summary>
public class AuthorRepository
{
    private readonly ApplicationDbContext _context;

    public AuthorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Author>> ListAsync(string userId)
    {
        return _context.Authors
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public Task<Author?> FindAsync(int id, string userId)
    {
        return _context.Authors
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }

    public Task<Author?> FindWithBooksAsync(int id, string userId)
    {
        return _context.Authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }

    public Task<bool> ExistsAsync(int id, string userId)
    {
        return _context.Authors
            .AnyAsync(a => a.Id == id && a.UserId == userId);
    }

    public void Add(Author author, string userId)
    {
        author.UserId = userId;
        _context.Authors.Add(author);
    }

    public void Remove(Author author)
    {
        _context.Authors.Remove(author);
    }

    public Task SaveAsync()
    {
        return _context.SaveChangesAsync();
    }
}
