using Bookshelf.Controllers;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.Repositories;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Bookshelf.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Bookshelf.Tests.Controllers;

public sealed class AuthorsControllerTests : IDisposable
{
    private const string UserId = RepositoryTestContext.DefaultUserId;

    private readonly ApplicationDbContext _context;
    private readonly AuthorRepository _repository;
    private readonly AuthorsController _controller;

    public AuthorsControllerTests()
    {
        _context = RepositoryTestContext.CreateDbContext();
        _repository = new AuthorRepository(_context, RepositoryTestContext.AccessorFor(UserId));
        _controller = new AuthorsController(_repository);
    }

    public void Dispose() { _controller.Dispose(); _context.Dispose(); GC.SuppressFinalize(this); }

    private async Task<Author> SeedAuthor(string name = "Ursula")
    {
        var author = new AuthorBuilder().WithName(name).WithUserId(UserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        return author;
    }

    [Fact]
    public async Task IndexReturnsViewWithAuthors()
    {
        await SeedAuthor();

        var view = Assert.IsType<ViewResult>(await _controller.Index());
        var model = Assert.IsAssignableFrom<List<Author>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task DetailsNullIdReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Details(null));
    }

    [Fact]
    public async Task DetailsAuthorMissingReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Details(999));
    }

    [Fact]
    public async Task DetailsAuthorFoundReturnsViewWithAuthor()
    {
        var author = await SeedAuthor();

        var view = Assert.IsType<ViewResult>(await _controller.Details(author.Id));
        var model = Assert.IsType<Author>(view.Model);
        Assert.Equal(author.Id, model.Id);
    }

    [Fact]
    public void CreateGetReturnsViewWithEmptyForm()
    {
        var view = Assert.IsType<ViewResult>(_controller.Create());
        var vm = Assert.IsType<AuthorFormViewModel>(view.Model);
        Assert.Equal(0, vm.Id);
    }

    [Fact]
    public async Task CreatePostValidRedirectsToIndexAndPersists()
    {
        var result = await _controller.Create(new AuthorFormViewModel { Name = "Ursula" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthorsController.Index), redirect.ActionName);
        Assert.Single(_context.Authors);
    }

    [Fact]
    public async Task CreatePostDuplicateNameReturnsViewWithoutRedirecting()
    {
        await SeedAuthor("Ursula");
        var vm = new AuthorFormViewModel { Name = "Ursula" };

        var result = await _controller.Create(vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
    }

    [Fact]
    public async Task CreatePostInvalidModelStateReturnsViewWithoutPersisting()
    {
        _controller.ModelState.AddModelError("Name", "required");
        var vm = new AuthorFormViewModel();

        var result = await _controller.Create(vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.Empty(_context.Authors);
    }

    [Fact]
    public async Task EditGetNullIdReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Edit((int?)null));
    }

    [Fact]
    public async Task EditGetAuthorMissingReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Edit((int?)999));
    }

    [Fact]
    public async Task EditGetAuthorFoundReturnsPopulatedView()
    {
        var author = await SeedAuthor("Ursula");

        var view = Assert.IsType<ViewResult>(await _controller.Edit((int?)author.Id));
        var vm = Assert.IsType<AuthorFormViewModel>(view.Model);
        Assert.Equal(author.Id, vm.Id);
        Assert.Equal("Ursula", vm.Name);
    }

    [Fact]
    public async Task EditPostRouteIdMismatchReturnsNotFound()
    {
        var result = await _controller.Edit(id: 1, new AuthorFormViewModel { Id = 2 });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPostValidAndOwnedPersistsAndRedirects()
    {
        var author = await SeedAuthor("Old");

        var result = await _controller.Edit(
            author.Id,
            new AuthorFormViewModel { Id = author.Id, Name = "New" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthorsController.Index), redirect.ActionName);
        Assert.Equal("New", (await _context.Authors.FindAsync(author.Id))!.Name);
    }

    [Fact]
    public async Task EditPostAuthorNotFoundReturnsNotFound()
    {
        // The repository returns NotFound when no author with the given id exists for this user.
        var result = await _controller.Edit(
            id: 999,
            new AuthorFormViewModel { Id = 999, Name = "X" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPostRenameToOtherAuthorsNameReturnsViewWithoutPersisting()
    {
        var a = await SeedAuthor("Alpha");
        var b = await SeedAuthor("Beta");
        var vm = new AuthorFormViewModel { Id = b.Id, Name = "Alpha" };

        var result = await _controller.Edit(b.Id, vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.Equal("Beta", (await _context.Authors.FindAsync(b.Id))!.Name);
    }

    [Fact]
    public async Task EditPostInvalidModelStateReturnsViewWithoutPersisting()
    {
        var author = await SeedAuthor("Old");
        _controller.ModelState.AddModelError("Name", "required");
        var vm = new AuthorFormViewModel { Id = author.Id, Name = string.Empty };

        var result = await _controller.Edit(author.Id, vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.Equal("Old", (await _context.Authors.FindAsync(author.Id))!.Name);
    }

    [Fact]
    public async Task DeleteGetNullIdReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Delete(null));
    }

    [Fact]
    public async Task DeleteGetAuthorMissingReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Delete(999));
    }

    [Fact]
    public async Task DeleteGetAuthorFoundReturnsViewWithAuthor()
    {
        var author = await SeedAuthor();

        var view = Assert.IsType<ViewResult>(await _controller.Delete(author.Id));
        Assert.IsType<Author>(view.Model);
    }

    [Fact]
    public async Task DeleteConfirmedAuthorMissingRedirectsWithoutDeleting()
    {
        var result = await _controller.DeleteConfirmed(999);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmedAuthorFoundDeletesAndRedirects()
    {
        var author = await SeedAuthor();

        var result = await _controller.DeleteConfirmed(author.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthorsController.Index), redirect.ActionName);
        Assert.Empty(_context.Authors);
    }
}
