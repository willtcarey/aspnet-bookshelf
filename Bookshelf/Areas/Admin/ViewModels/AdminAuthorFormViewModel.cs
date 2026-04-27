using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookshelf.Areas.Admin.ViewModels;

public class AdminAuthorFormViewModel : IAdminFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public SelectList? Users { get; set; }
}
