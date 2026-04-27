using Bookshelf.Areas.Admin;
using Microsoft.AspNetCore.Identity;

namespace Bookshelf.Models;

public class Author : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<Book> Books { get; set; } = new();

    // Owner of this author. Authors are NOT shared between users -- if two users
    // want "Ursula K. Le Guin", they each create their own row. Set server-side
    // in controllers; never trust a UserId coming from form input.
    public string UserId { get; set; } = string.Empty;
    public IdentityUser User { get; set; } = null!;
}
