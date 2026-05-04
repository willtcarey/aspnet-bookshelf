using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.Areas.Admin.ViewModels;

namespace Bookshelf.Areas.Admin.Controllers;

public class AuthorsController : AdminCrudController<Author, AdminAuthorFormViewModel>
{
    private readonly UserManager<IdentityUser> _userManager;

    public AuthorsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        : base(context)
    {
        _userManager = userManager;
    }

    protected override DbSet<Author> DbSet => Context.Authors;
    protected override IQueryable<Author> GetBaseQuery() =>
        Context.Authors.Include(a => a.Books).Include(a => a.User);
    protected override Dictionary<string, Expression<Func<Author, object?>>> SortMap => new()
    {
        ["name"] = a => a.Name,
        ["owner"] = a => a.User.Email!
    };
    protected override Expression<Func<Author, object?>> DefaultSort => a => a.Name;

    protected override AdminAuthorFormViewModel MapToViewModel(Author entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        UserId = entity.UserId
    };

    protected override Author CreateEntity(AdminAuthorFormViewModel viewModel) => new()
    {
        Name = viewModel.Name,
        UserId = viewModel.UserId
    };

    protected override void UpdateEntity(Author entity, AdminAuthorFormViewModel viewModel)
    {
        entity.Name = viewModel.Name;
        entity.UserId = viewModel.UserId;
    }

    protected override async Task PopulateFormDataAsync(AdminAuthorFormViewModel viewModel)
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        viewModel.Users = new SelectList(users, "Id", "Email", viewModel.UserId);
    }
}
