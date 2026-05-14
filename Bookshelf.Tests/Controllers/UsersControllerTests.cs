using System.Security.Claims;
using Bookshelf.Areas.Admin.Controllers;
using Bookshelf.Areas.Admin.ViewModels;
using Bookshelf.Data;
using Bookshelf.Helpers;
using Bookshelf.Security;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Bookshelf.Tests.Controllers;

public class UsersControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _userManager;
    private readonly Mock<RoleManager<IdentityRole>> _roleManager;
    private readonly UsersController _controller;
    private const string CurrentAdminId = "admin-1";

    public UsersControllerTests()
    {
        _context = RepositoryTestContext.CreateDbContext();
        _userManager = IdentityMocks.UserManager();
        _roleManager = IdentityMocks.RoleManager();
        _userManager.Setup(m => m.Users).Returns(() => _context.Users);
        _controller = new UsersController(_userManager.Object, _roleManager.Object);
        AttachCurrentUser(CurrentAdminId);
    }

    public void Dispose() => _context.Dispose();

    private void AttachCurrentUser(string userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            authenticationType: "Test"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private async Task<IdentityUser> SeedUser(string id, string email)
    {
        var user = new IdentityUser
        {
            Id = id,
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant()
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Index_DefaultSort_OrdersByEmailAscending()
    {
        await SeedUser("u1", "charlie@example.com");
        await SeedUser("u2", "alpha@example.com");
        _userManager.Setup(m => m.IsInRoleAsync(It.IsAny<IdentityUser>(), RoleNames.Admin)).ReturnsAsync(false);

        var view = Assert.IsType<ViewResult>(await _controller.Index());
        var list = Assert.IsType<PaginatedList<AdminUserViewModel>>(view.Model);
        Assert.Equal("alpha@example.com", list[0].Email);
    }

    [Fact]
    public async Task Index_EmailDescending_OrdersByEmailDescending()
    {
        await SeedUser("u1", "alpha@example.com");
        await SeedUser("u2", "charlie@example.com");
        _userManager.Setup(m => m.IsInRoleAsync(It.IsAny<IdentityUser>(), RoleNames.Admin)).ReturnsAsync(false);

        var view = Assert.IsType<ViewResult>(await _controller.Index(sort: "email", dir: "desc"));
        var list = Assert.IsType<PaginatedList<AdminUserViewModel>>(view.Model);
        Assert.Equal("charlie@example.com", list[0].Email);
    }

    [Fact]
    public async Task Index_EmailAscendingExplicit_OrdersByEmailAscending()
    {
        await SeedUser("u1", "charlie@example.com");
        await SeedUser("u2", "alpha@example.com");
        _userManager.Setup(m => m.IsInRoleAsync(It.IsAny<IdentityUser>(), RoleNames.Admin)).ReturnsAsync(false);

        var view = Assert.IsType<ViewResult>(await _controller.Index(sort: "email", dir: "asc"));
        var list = Assert.IsType<PaginatedList<AdminUserViewModel>>(view.Model);
        Assert.Equal("alpha@example.com", list[0].Email);
    }

    [Fact]
    public async Task Index_PopulatesIsAdminFlag()
    {
        var admin = await SeedUser("u1", "admin@example.com");
        var regular = await SeedUser("u2", "user@example.com");
        _userManager.Setup(m => m.IsInRoleAsync(admin, RoleNames.Admin)).ReturnsAsync(true);
        _userManager.Setup(m => m.IsInRoleAsync(regular, RoleNames.Admin)).ReturnsAsync(false);

        var view = Assert.IsType<ViewResult>(await _controller.Index());
        var list = Assert.IsType<PaginatedList<AdminUserViewModel>>(view.Model);
        Assert.True(list.First(v => v.Id == "u1").IsAdmin);
        Assert.False(list.First(v => v.Id == "u2").IsAdmin);
    }

    [Fact]
    public async Task ToggleAdmin_TargetingSelf_RedirectsWithoutMutatingRoles()
    {
        var result = await _controller.ToggleAdmin(CurrentAdminId);

        Assert.IsType<RedirectToActionResult>(result);
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
        _userManager.Verify(m => m.RemoveFromRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleAdmin_UserMissing_ReturnsNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((IdentityUser?)null);

        var result = await _controller.ToggleAdmin("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ToggleAdmin_UserIsAdmin_RemovesAdminRoleAndRedirects()
    {
        var target = new IdentityUser { Id = "u-target", Email = "x@y.com" };
        _userManager.Setup(m => m.FindByIdAsync("u-target")).ReturnsAsync(target);
        _userManager.Setup(m => m.IsInRoleAsync(target, RoleNames.Admin)).ReturnsAsync(true);
        _userManager.Setup(m => m.RemoveFromRoleAsync(target, RoleNames.Admin))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ToggleAdmin("u-target");

        Assert.IsType<RedirectToActionResult>(result);
        _userManager.Verify(m => m.RemoveFromRoleAsync(target, RoleNames.Admin), Times.Once);
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleAdmin_UserIsNotAdmin_AddsAdminRoleAndRedirects()
    {
        var target = new IdentityUser { Id = "u-target", Email = "x@y.com" };
        _userManager.Setup(m => m.FindByIdAsync("u-target")).ReturnsAsync(target);
        _userManager.Setup(m => m.IsInRoleAsync(target, RoleNames.Admin)).ReturnsAsync(false);
        _userManager.Setup(m => m.AddToRoleAsync(target, RoleNames.Admin))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ToggleAdmin("u-target");

        Assert.IsType<RedirectToActionResult>(result);
        _userManager.Verify(m => m.AddToRoleAsync(target, RoleNames.Admin), Times.Once);
        _userManager.Verify(m => m.RemoveFromRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
    }
}
