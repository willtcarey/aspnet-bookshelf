using Bookshelf.Areas.Admin;
using Bookshelf.Attributes;
using Microsoft.AspNetCore.Identity;

namespace Bookshelf.Models;

public class Book : IEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public int? Year { get; set; }

    [FileAttachment]
    public string? CoverImagePath { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    // Owner of this book. Required -- every book belongs to exactly one user.
    // Set server-side in controllers; never trust a UserId coming from form input.
    public string UserId { get; set; } = string.Empty;
    public IdentityUser User { get; set; } = null!;
}
