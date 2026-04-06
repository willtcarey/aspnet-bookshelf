using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookshelf.ViewModels;

public class BookFormViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public int? Year { get; set; }
    public int AuthorId { get; set; }
    public string? ExistingCoverImagePath { get; set; }

    [Display(Name = "Cover image")]
    public IFormFile? CoverImage { get; set; }

    public SelectList? Authors { get; set; }
}
