using Bookshelf.Controllers;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.Repositories;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Bookshelf.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Bookshelf.Tests.Controllers;

public sealed class BooksControllerTests : IDisposable
{
    private const string UserId = RepositoryTestContext.DefaultUserId;
    private const string OtherUserId = "other-user-id";

    private readonly ApplicationDbContext _context;
    private readonly BookRepository _repository;
    private readonly BooksController _controller;

    public BooksControllerTests()
    {
        _context = RepositoryTestContext.CreateDbContext();
        _repository = new BookRepository(_context, RepositoryTestContext.AccessorFor(UserId));
        _controller = new BooksController(_repository);
    }

    public void Dispose() { _controller.Dispose(); _context.Dispose(); GC.SuppressFinalize(this); }

    private async Task<Author> SeedAuthor(string userId = UserId, string name = "Ursula")
    {
        var author = new AuthorBuilder().WithUserId(userId).WithName(name).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        return author;
    }

    private async Task<Book> SeedBook(Author author, string title = "A Wizard of Earthsea")
    {
        var book = new BookBuilder().WithAuthorId(author.Id).WithTitle(title).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return book;
    }

    [Fact]
    public async Task IndexReturnsViewWithBooks()
    {
        var author = await SeedAuthor();
        await SeedBook(author);

        var view = Assert.IsType<ViewResult>(await _controller.Index());
        var model = Assert.IsAssignableFrom<List<Book>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task DetailsNullIdReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Details(null));
    }

    [Fact]
    public async Task DetailsBookMissingReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Details(999));
    }

    [Fact]
    public async Task DetailsBookFoundReturnsViewWithBook()
    {
        var author = await SeedAuthor();
        var book = await SeedBook(author);

        var view = Assert.IsType<ViewResult>(await _controller.Details(book.Id));
        var model = Assert.IsType<Book>(view.Model);
        Assert.Equal(book.Id, model.Id);
    }

    [Fact]
    public async Task CreateGetReturnsViewWithFormAndAuthorList()
    {
        await SeedAuthor(name: "Alpha");

        var view = Assert.IsType<ViewResult>(await _controller.Create());
        var vm = Assert.IsType<BookFormViewModel>(view.Model);
        Assert.NotNull(vm.Authors);
        Assert.Single(vm.Authors!);
    }

    [Fact]
    public async Task CreatePostValidRedirectsAndPersists()
    {
        var author = await SeedAuthor();

        var result = await _controller.Create(new BookFormViewModel
        {
            Title = "A Wizard of Earthsea",
            AuthorId = author.Id,
            Year = 1968
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(BooksController.Index), redirect.ActionName);
        Assert.Single(_context.Books);
    }

    [Fact]
    public async Task CreatePostRepositoryRejectsAuthorReturnsViewWithAuthorList()
    {
        // The author belongs to a different user, so BookRepository.CreateAsync
        // returns ValidationFailed and the controller should fall through to View.
        var foreignAuthor = await SeedAuthor(OtherUserId, "Foreign");
        var vm = new BookFormViewModel { Title = "X", AuthorId = foreignAuthor.Id };

        var result = await _controller.Create(vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.NotNull(vm.Authors);
        Assert.Empty(_context.Books);
    }

    [Fact]
    public async Task CreatePostInvalidModelStateReturnsViewWithAuthorList()
    {
        await SeedAuthor();
        _controller.ModelState.AddModelError("Title", "required");
        var vm = new BookFormViewModel();

        var result = await _controller.Create(vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.NotNull(vm.Authors);
        Assert.Empty(_context.Books);
    }

    [Fact]
    public async Task EditGetNullIdReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Edit((int?)null));
    }

    [Fact]
    public async Task EditGetBookMissingReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Edit((int?)999));
    }

    [Fact]
    public async Task EditGetBookFoundReturnsPopulatedView()
    {
        var author = await SeedAuthor();
        var book = await SeedBook(author, "Earthsea");

        var view = Assert.IsType<ViewResult>(await _controller.Edit((int?)book.Id));
        var vm = Assert.IsType<BookFormViewModel>(view.Model);
        Assert.Equal(book.Id, vm.Id);
        Assert.Equal("Earthsea", vm.Title);
        Assert.NotNull(vm.Authors);
    }

    [Fact]
    public async Task EditPostRouteIdMismatchReturnsNotFound()
    {
        var result = await _controller.Edit(id: 1, new BookFormViewModel { Id = 2 });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPostValidPersistsAndRedirects()
    {
        var author = await SeedAuthor();
        var book = await SeedBook(author, "Old");

        var result = await _controller.Edit(
            book.Id,
            new BookFormViewModel { Id = book.Id, Title = "New", AuthorId = author.Id, Year = 2026 });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(BooksController.Index), redirect.ActionName);
        Assert.Equal("New", (await _context.Books.FindAsync(book.Id))!.Title);
    }

    [Fact]
    public async Task EditPostBookNotFoundReturnsNotFound()
    {
        var author = await SeedAuthor();

        var result = await _controller.Edit(
            id: 999,
            new BookFormViewModel { Id = 999, Title = "X", AuthorId = author.Id });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPostAuthorReassignmentRejectedReturnsViewWithAuthorList()
    {
        var mineAuthor = await SeedAuthor(name: "Mine");
        var foreignAuthor = await SeedAuthor(OtherUserId, "Foreign");
        var book = await SeedBook(mineAuthor);

        var vm = new BookFormViewModel { Id = book.Id, Title = "X", AuthorId = foreignAuthor.Id };
        var result = await _controller.Edit(book.Id, vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.NotNull(vm.Authors);
    }

    [Fact]
    public async Task EditPostInvalidModelStateReturnsViewWithAuthorList()
    {
        var author = await SeedAuthor();
        var book = await SeedBook(author);
        _controller.ModelState.AddModelError("Title", "required");
        var vm = new BookFormViewModel { Id = book.Id, AuthorId = author.Id };

        var result = await _controller.Edit(book.Id, vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.NotNull(vm.Authors);
    }

    [Fact]
    public async Task DeleteGetNullIdReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Delete(null));
    }

    [Fact]
    public async Task DeleteGetBookMissingReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Delete(999));
    }

    [Fact]
    public async Task DeleteGetBookFoundReturnsViewWithBook()
    {
        var author = await SeedAuthor();
        var book = await SeedBook(author);

        var view = Assert.IsType<ViewResult>(await _controller.Delete(book.Id));
        Assert.IsType<Book>(view.Model);
    }

    [Fact]
    public async Task DeleteConfirmedBookMissingRedirectsWithoutDeleting()
    {
        var result = await _controller.DeleteConfirmed(999);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmedBookFoundDeletesAndRedirects()
    {
        var author = await SeedAuthor();
        var book = await SeedBook(author);

        var result = await _controller.DeleteConfirmed(book.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(BooksController.Index), redirect.ActionName);
        Assert.Empty(_context.Books);
    }
}
