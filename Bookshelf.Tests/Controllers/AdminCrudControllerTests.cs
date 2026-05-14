using System.Linq.Expressions;
using Bookshelf.Areas.Admin.Controllers;
using Bookshelf.Areas.Admin.ViewModels;
using Bookshelf.Data;
using Bookshelf.Helpers;
using Bookshelf.Models;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookshelf.Tests.Controllers;

public class AdminCrudControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestAdminCrudController _controller;

    public AdminCrudControllerTests()
    {
        _context = RepositoryTestContext.CreateDbContext();
        _controller = new TestAdminCrudController(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task<Author> SeedAuthor(string name = "Charlie", string userId = "owner-1")
    {
        var author = new AuthorBuilder().WithName(name).WithUserId(userId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        return author;
    }

    [Fact]
    public async Task Index_NoSort_UsesDefaultSort()
    {
        await SeedAuthor("Charlie");
        await SeedAuthor("Alpha");

        var view = Assert.IsType<ViewResult>(await _controller.Index());
        var list = Assert.IsType<PaginatedList<Author>>(view.Model);
        Assert.Equal("Alpha", list[0].Name);
        Assert.Equal("Charlie", list[1].Name);
    }

    [Fact]
    public async Task Index_KnownSortAscending_OrdersAscending()
    {
        await SeedAuthor("Charlie");
        await SeedAuthor("Alpha");

        var view = Assert.IsType<ViewResult>(await _controller.Index(sort: "name", dir: "asc"));
        var list = Assert.IsType<PaginatedList<Author>>(view.Model);
        Assert.Equal("Alpha", list[0].Name);
    }

    [Fact]
    public async Task Index_KnownSortDescending_OrdersDescending()
    {
        await SeedAuthor("Charlie");
        await SeedAuthor("Alpha");

        var view = Assert.IsType<ViewResult>(await _controller.Index(sort: "name", dir: "desc"));
        var list = Assert.IsType<PaginatedList<Author>>(view.Model);
        Assert.Equal("Charlie", list[0].Name);
    }

    [Fact]
    public async Task Index_UnknownSort_FallsBackToDefault()
    {
        await SeedAuthor("Charlie");
        await SeedAuthor("Alpha");

        var view = Assert.IsType<ViewResult>(await _controller.Index(sort: "not-a-column"));
        var list = Assert.IsType<PaginatedList<Author>>(view.Model);
        Assert.Equal("Alpha", list[0].Name);
    }

    [Fact]
    public async Task Create_Get_ReturnsViewWithEmptyForm()
    {
        var view = Assert.IsType<ViewResult>(await _controller.Create());
        var vm = Assert.IsType<AdminAuthorFormViewModel>(view.Model);
        Assert.Equal(0, vm.Id);
    }

    [Fact]
    public async Task Create_Post_Valid_RedirectsAndPersists()
    {
        var vm = new AdminAuthorFormViewModel { Name = "New", UserId = "owner-1" };

        var result = await _controller.Create(vm);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Single(_context.Authors);
    }

    [Fact]
    public async Task Create_Post_InvalidModelState_ReturnsViewWithoutPersisting()
    {
        _controller.ModelState.AddModelError("Name", "required");
        var vm = new AdminAuthorFormViewModel();

        var result = await _controller.Create(vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.Empty(_context.Authors);
    }

    [Fact]
    public async Task Edit_Get_NullId_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Edit((int?)null));
    }

    [Fact]
    public async Task Edit_Get_EntityMissing_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Edit((int?)999));
    }

    [Fact]
    public async Task Edit_Get_EntityFound_ReturnsPopulatedView()
    {
        var author = await SeedAuthor("X");

        var view = Assert.IsType<ViewResult>(await _controller.Edit((int?)author.Id));
        var vm = Assert.IsType<AdminAuthorFormViewModel>(view.Model);
        Assert.Equal(author.Id, vm.Id);
        Assert.Equal("X", vm.Name);
    }

    [Fact]
    public async Task Edit_Post_RouteIdMismatch_ReturnsNotFound()
    {
        var result = await _controller.Edit(id: 1, new AdminAuthorFormViewModel { Id = 2 });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_EntityMissing_ReturnsNotFound()
    {
        var result = await _controller.Edit(
            id: 999,
            new AdminAuthorFormViewModel { Id = 999, Name = "X", UserId = "owner-1" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Valid_PersistsAndRedirects()
    {
        var author = await SeedAuthor("Old");

        var result = await _controller.Edit(
            author.Id,
            new AdminAuthorFormViewModel { Id = author.Id, Name = "New", UserId = "owner-1" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("New", (await _context.Authors.FindAsync(author.Id))!.Name);
    }

    [Fact]
    public async Task Edit_Post_InvalidModelState_ReturnsViewWithoutPersisting()
    {
        var author = await SeedAuthor("Old");
        _controller.ModelState.AddModelError("Name", "required");
        var vm = new AdminAuthorFormViewModel { Id = author.Id };

        var result = await _controller.Edit(author.Id, vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.Equal("Old", (await _context.Authors.FindAsync(author.Id))!.Name);
    }

    [Fact]
    public async Task Edit_Post_ConcurrencyException_EntityDisappeared_ReturnsNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        using var setupContext = RepositoryTestContext.CreateDbContext(dbName);
        var author = new AuthorBuilder().WithName("Old").WithUserId("owner-1").Build();
        setupContext.Authors.Add(author);
        await setupContext.SaveChangesAsync();

        using var throwing = RepositoryTestContext.CreateDbContext<ThrowingApplicationDbContext>(
            dbName,
            factory: (options, storage) => new ThrowingApplicationDbContext(options, storage));
        throwing.ShouldThrowOnSave = true;
        throwing.OnBeforeThrow = async () =>
        {
            using var sideContext = RepositoryTestContext.CreateDbContext(dbName);
            var existing = await sideContext.Authors.FindAsync(author.Id);
            sideContext.Authors.Remove(existing!);
            await sideContext.SaveChangesAsync();
        };
        var controller = new TestAdminCrudController(throwing);

        var result = await controller.Edit(
            author.Id,
            new AdminAuthorFormViewModel { Id = author.Id, Name = "New", UserId = "owner-1" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ConcurrencyException_EntityStillExists_Rethrows()
    {
        var dbName = Guid.NewGuid().ToString();
        using var setupContext = RepositoryTestContext.CreateDbContext(dbName);
        var author = new AuthorBuilder().WithName("Old").WithUserId("owner-1").Build();
        setupContext.Authors.Add(author);
        await setupContext.SaveChangesAsync();

        using var throwing = RepositoryTestContext.CreateDbContext<ThrowingApplicationDbContext>(
            dbName,
            factory: (options, storage) => new ThrowingApplicationDbContext(options, storage));
        throwing.ShouldThrowOnSave = true;
        var controller = new TestAdminCrudController(throwing);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => controller.Edit(
                author.Id,
                new AdminAuthorFormViewModel { Id = author.Id, Name = "New", UserId = "owner-1" }));
    }

    [Fact]
    public async Task Delete_Get_NullId_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Delete(null));
    }

    [Fact]
    public async Task Delete_Get_EntityMissing_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Delete(999));
    }

    [Fact]
    public async Task Delete_Get_EntityFound_ReturnsViewWithEntity()
    {
        var author = await SeedAuthor();

        var view = Assert.IsType<ViewResult>(await _controller.Delete(author.Id));
        Assert.IsType<Author>(view.Model);
    }

    [Fact]
    public async Task DeleteConfirmed_EntityMissing_RedirectsWithoutDeleting()
    {
        var result = await _controller.DeleteConfirmed(999);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_EntityFound_DeletesAndRedirects()
    {
        var author = await SeedAuthor();

        var result = await _controller.DeleteConfirmed(author.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Empty(_context.Authors);
    }

    /// <summary>
    /// Concrete subclass that exercises the generic AdminCrudController base
    /// against the Author entity without the production AuthorsController's
    /// UserManager-driven PopulateFormDataAsync override.
    /// </summary>
    private sealed class TestAdminCrudController : AdminCrudController<Author, AdminAuthorFormViewModel>
    {
        public TestAdminCrudController(ApplicationDbContext context) : base(context) { }

        protected override DbSet<Author> DbSet => Context.Authors;
        protected override IQueryable<Author> GetBaseQuery() => Context.Authors;
        protected override Dictionary<string, Expression<Func<Author, object?>>> SortMap => new()
        {
            ["name"] = a => a.Name
        };
        protected override Expression<Func<Author, object?>> DefaultSort => a => a.Name;

        protected override AdminAuthorFormViewModel MapToViewModel(Author entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            UserId = entity.UserId
        };

        protected override Author CreateEntity(AdminAuthorFormViewModel vm) => new()
        {
            Name = vm.Name,
            UserId = vm.UserId
        };

        protected override void UpdateEntity(Author entity, AdminAuthorFormViewModel vm)
        {
            entity.Name = vm.Name;
            entity.UserId = vm.UserId;
        }
    }
}
