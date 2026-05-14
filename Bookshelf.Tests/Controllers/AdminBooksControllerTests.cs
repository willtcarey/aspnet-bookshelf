using Bookshelf.Areas.Admin.ViewModels;
using Bookshelf.Data;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AdminBooksController = Bookshelf.Areas.Admin.Controllers.BooksController;

namespace Bookshelf.Tests.Controllers;

public class AdminBooksControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AdminBooksController _controller;

    public AdminBooksControllerTests()
    {
        _context = RepositoryTestContext.CreateDbContext();
        _controller = new AdminBooksController(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task<(IdentityUser User, Bookshelf.Models.Author Author)> SeedAuthorWithUser(string email = "owner@example.com", string authorName = "Ursula")
    {
        var user = new IdentityUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant()
        };
        _context.Users.Add(user);
        var author = new AuthorBuilder().WithName(authorName).WithUserId(user.Id).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        return (user, author);
    }

    [Fact]
    public async Task Create_Get_PopulatesAuthorsSelectListOrderedByName()
    {
        await SeedAuthorWithUser("o1@example.com", "Charlie");
        await SeedAuthorWithUser("o2@example.com", "Alpha");

        var view = Assert.IsType<ViewResult>(await _controller.Create());
        var vm = Assert.IsType<AdminBookFormViewModel>(view.Model);
        var items = Assert.IsType<SelectList>(vm.Authors).ToList();
        Assert.StartsWith("Alpha", items[0].Text);
        Assert.StartsWith("Charlie", items[1].Text);
    }

    [Fact]
    public async Task Create_Post_Valid_PersistsBook()
    {
        var (_, author) = await SeedAuthorWithUser();

        var result = await _controller.Create(new AdminBookFormViewModel
        {
            Title = "Earthsea",
            AuthorId = author.Id,
            Year = 1968
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Single(_context.Books);
    }

    [Fact]
    public async Task Edit_Get_PopulatesViewModelFromEntity()
    {
        var (_, author) = await SeedAuthorWithUser();
        var book = new BookBuilder().WithTitle("Earthsea").WithAuthorId(author.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await _controller.Edit((int?)book.Id));
        var vm = Assert.IsType<AdminBookFormViewModel>(view.Model);
        Assert.Equal("Earthsea", vm.Title);
    }

    [Fact]
    public async Task Edit_Post_Valid_PersistsUpdates()
    {
        var (_, author) = await SeedAuthorWithUser();
        var book = new BookBuilder().WithTitle("Old").WithAuthorId(author.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var result = await _controller.Edit(
            book.Id,
            new AdminBookFormViewModel { Id = book.Id, Title = "New", AuthorId = author.Id });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("New", (await _context.Books.FindAsync(book.Id))!.Title);
    }
}
