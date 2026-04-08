namespace Bookshelf.Areas.Admin.ViewModels;

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
}
