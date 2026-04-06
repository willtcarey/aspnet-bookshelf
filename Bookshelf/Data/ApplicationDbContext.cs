using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Models;

namespace Bookshelf.Data;

// We inherit from IdentityDbContext to keep app models and identity in a single context.
// These could be separated into multiple contexts, but we'd lose the ability to create
// navigation properties between our models and IdentityUser. Separate contexts would be
// preferred if our users aren't database-backed in the same way (e.g., external auth).
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
}
