using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookshelf.ViewModels;

public class BookFormViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public int? Year { get; set; }
    public int AuthorId { get; set; }

    public SelectList? Authors { get; set; }
}
