using Bookshelf.Areas.Admin.ViewModels;
using Bookshelf.Data;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using AdminAuthorsController = Bookshelf.Areas.Admin.Controllers.AuthorsController;

namespace Bookshelf.Tests.Controllers;

public sealed class AdminAuthorsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _userManager;
    private readonly AdminAuthorsController _controller;

    public AdminAuthorsControllerTests()
    {
        _context = RepositoryTestContext.CreateDbContext();
        _userManager = IdentityMocks.UserManager();
        _userManager.Setup(m => m.Users).Returns(() => _context.Users);
        _controller = new AdminAuthorsController(_context, _userManager.Object);
    }

    public void Dispose() { _controller.Dispose(); _context.Dispose(); GC.SuppressFinalize(this); }

    private async Task SeedUser(string id, string email)
    {
        _context.Users.Add(new IdentityUser
        {
            Id = id,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant()
        });
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateGetPopulatesUsersSelectListOrderedByEmail()
    {
        await SeedUser("u1", "charlie@example.com");
        await SeedUser("u2", "alpha@example.com");

        var view = Assert.IsType<ViewResult>(await _controller.Create());
        var vm = Assert.IsType<AdminAuthorFormViewModel>(view.Model);
        var items = Assert.IsType<SelectList>(vm.Users).ToList();
        Assert.Equal("alpha@example.com", items[0].Text);
        Assert.Equal("charlie@example.com", items[1].Text);
    }

    [Fact]
    public async Task CreatePostValidPersistsAuthorWithSelectedUser()
    {
        await SeedUser("u1", "a@b.com");

        var result = await _controller.Create(new AdminAuthorFormViewModel { Name = "New", UserId = "u1" });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Single(_context.Authors);
    }

    [Fact]
    public async Task EditGetPopulatesViewModelFromEntity()
    {
        await SeedUser("u1", "a@b.com");
        var author = new AuthorBuilder().WithName("Ursula").WithUserId("u1").Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await _controller.Edit((int?)author.Id));
        var vm = Assert.IsType<AdminAuthorFormViewModel>(view.Model);
        Assert.Equal("Ursula", vm.Name);
        Assert.Equal("u1", vm.UserId);
    }

    [Fact]
    public async Task EditPostValidPersistsUpdates()
    {
        await SeedUser("u1", "a@b.com");
        var author = new AuthorBuilder().WithName("Old").WithUserId("u1").Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await _controller.Edit(
            author.Id,
            new AdminAuthorFormViewModel { Id = author.Id, Name = "New", UserId = "u1" });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("New", (await _context.Authors.FindAsync(author.Id))!.Name);
    }

    [Fact]
    public async Task IndexExercisesGetBaseQueryAndDefaultSort()
    {
        await SeedUser("u1", "a@b.com");
        _context.Authors.Add(new AuthorBuilder().WithName("Charlie").WithUserId("u1").Build());
        _context.Authors.Add(new AuthorBuilder().WithName("Alpha").WithUserId("u1").Build());
        await _context.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await _controller.Index());

        Assert.NotNull(view.Model);
    }

    [Fact]
    public async Task IndexExercisesSortMapName()
    {
        await SeedUser("u1", "a@b.com");
        _context.Authors.Add(new AuthorBuilder().WithName("Charlie").WithUserId("u1").Build());
        _context.Authors.Add(new AuthorBuilder().WithName("Alpha").WithUserId("u1").Build());
        await _context.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await _controller.Index(sort: "name", dir: "asc"));

        Assert.NotNull(view.Model);
    }

    [Fact]
    public async Task DeleteGetExercisesGetBaseQuery()
    {
        await SeedUser("u1", "a@b.com");
        var author = new AuthorBuilder().WithName("X").WithUserId("u1").Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await _controller.Delete(author.Id));

        Assert.IsType<Bookshelf.Models.Author>(view.Model);
    }
}
