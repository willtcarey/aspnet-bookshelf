using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookshelf.Areas.Admin.ViewModels;

public class AdminBookFormViewModel : IAdminFormViewModel
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

    [Required]
    public string UserId { get; set; } = string.Empty;

    public SelectList? Authors { get; set; }
    public SelectList? Users { get; set; }
}
