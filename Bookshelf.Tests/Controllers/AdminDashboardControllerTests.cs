using Bookshelf.Areas.Admin.Controllers;
using Bookshelf.Data;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Bookshelf.Tests.Controllers;

public class AdminDashboardControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _userManager;
    private readonly DashboardController _controller;

    public AdminDashboardControllerTests()
    {
        _context = RepositoryTestContext.CreateDbContext();
        _userManager = IdentityMocks.UserManager();
        _userManager.Setup(m => m.Users).Returns(() => _context.Users);
        _controller = new DashboardController(_context, _userManager.Object);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Index_PopulatesCountsInViewData()
    {
        var user = new IdentityUser
        {
            Id = "u1",
            Email = "x@y.com",
            UserName = "x@y.com",
            NormalizedEmail = "X@Y.COM",
            NormalizedUserName = "X@Y.COM"
        };
        _context.Users.Add(user);
        var author = new AuthorBuilder().WithUserId(user.Id).Build();
        _context.Authors.Add(author);
        _context.Books.Add(new BookBuilder().WithAuthorId(author.Id).Build());
        _context.Books.Add(new BookBuilder().WithAuthorId(author.Id).WithTitle("Other").Build());
        await _context.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await _controller.Index());

        Assert.Equal(2, view.ViewData["BookCount"]);
        Assert.Equal(1, view.ViewData["AuthorCount"]);
        Assert.Equal(1, view.ViewData["UserCount"]);
    }
}
