using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.Areas.Admin.ViewModels;

namespace Bookshelf.Areas.Admin.Controllers;

public class BooksController : AdminCrudController<Book, AdminBookFormViewModel>
{
    public BooksController(ApplicationDbContext context) : base(context) { }

    protected override DbSet<Book> DbSet => Context.Books;
    protected override IQueryable<Book> GetBaseQuery() =>
        Context.Books.Include(b => b.Author).ThenInclude(a => a.User);
    protected override Dictionary<string, Expression<Func<Book, object?>>> SortMap => new()
    {
        ["title"] = b => b.Title,
        ["author"] = b => b.Author.Name,
        ["owner"] = b => b.Author.User.Email!,
        ["year"] = b => (object?)b.Year
    };
    protected override Expression<Func<Book, object?>> DefaultSort => b => b.Title;

    protected override AdminBookFormViewModel MapToViewModel(Book entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Isbn = entity.Isbn,
        Year = entity.Year,
        AuthorId = entity.AuthorId,
        CoverImagePath = entity.CoverImagePath
    };

    protected override Book CreateEntity(AdminBookFormViewModel vm) => new()
    {
        Title = vm.Title,
        Isbn = vm.Isbn,
        Year = vm.Year,
        AuthorId = vm.AuthorId,
        CoverImagePath = vm.CoverImagePath
    };

    protected override void UpdateEntity(Book entity, AdminBookFormViewModel vm)
    {
        entity.Title = vm.Title;
        entity.Isbn = vm.Isbn;
        entity.Year = vm.Year;
        entity.AuthorId = vm.AuthorId;
        entity.CoverImagePath = vm.CoverImagePath;
    }

    protected override async Task PopulateFormDataAsync(AdminBookFormViewModel vm)
    {
        var authors = await Context.Authors
            .Include(a => a.User)
            .OrderBy(a => a.Name)
            .Select(a => new { a.Id, Label = $"{a.Name} ({a.User.Email})" })
            .ToListAsync();
        vm.Authors = new SelectList(authors, "Id", "Label", vm.AuthorId);
    }
}
