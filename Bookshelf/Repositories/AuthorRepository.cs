using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;

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

    public Task<bool> ExistsAsync(int id)
    {
        return _context.Authors
            .AnyAsync(a => a.Id == id && a.UserId == _userId);
    }

    public void Add(Author author)
    {
        author.UserId = _userId;
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
