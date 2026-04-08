namespace Bookshelf.Areas.Admin.ViewModels;

public class AdminAuthorFormViewModel : IAdminFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
