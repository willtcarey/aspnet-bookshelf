using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.Areas.Admin.ViewModels;

namespace Bookshelf.Areas.Admin.Controllers;

public class AuthorsController : AdminCrudController<Author, AdminAuthorFormViewModel>
{
    public AuthorsController(ApplicationDbContext context) : base(context) { }

    protected override DbSet<Author> DbSet => Context.Authors;
    protected override IQueryable<Author> GetBaseQuery() => Context.Authors.Include(a => a.Books);
    protected override Dictionary<string, Expression<Func<Author, object?>>> SortMap => new()
    {
        ["name"] = a => a.Name
    };
    protected override Expression<Func<Author, object?>> DefaultSort => a => a.Name;

    protected override AdminAuthorFormViewModel MapToViewModel(Author entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name
    };

    protected override Author CreateEntity(AdminAuthorFormViewModel vm) => new()
    {
        Name = vm.Name
    };

    protected override void UpdateEntity(Author entity, AdminAuthorFormViewModel vm)
    {
        entity.Name = vm.Name;
    }
}
