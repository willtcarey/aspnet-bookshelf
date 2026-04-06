namespace Bookshelf.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public int? Year { get; set; }
    public string? CoverImagePath { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
}
