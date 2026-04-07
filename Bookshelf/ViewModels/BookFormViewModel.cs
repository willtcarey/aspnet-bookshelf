using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookshelf.ViewModels;

public class BookFormViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public int? Year { get; set; }
    public int AuthorId { get; set; }

    /// <summary>
    /// Storage path for the cover image, submitted via a hidden field after direct upload.
    /// </summary>
    public string? CoverImagePath { get; set; }

    public SelectList? Authors { get; set; }
}
