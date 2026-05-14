using Bookshelf.Controllers;
using Bookshelf.Tests.TestSupport;
using Bookshelf.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Bookshelf.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<UserManager<IdentityUser>> _userManager;
    private readonly Mock<SignInManager<IdentityUser>> _signInManager;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _userManager = IdentityMocks.UserManager();
        _signInManager = IdentityMocks.SignInManager(_userManager.Object);
        _controller = new AccountController(_userManager.Object, _signInManager.Object);
    }

    [Fact]
    public void Register_Get_ReturnsView()
    {
        Assert.IsType<ViewResult>(_controller.Register());
    }

    [Fact]
    public async Task Register_Post_ValidAndCreated_SignsInAndRedirectsToHome()
    {
        var model = new RegisterViewModel { Email = "a@b.com", Password = "Password1!" };
        _userManager.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), model.Password))
            .ReturnsAsync(IdentityResult.Success);
        _signInManager.Setup(m => m.SignInAsync(It.IsAny<IdentityUser>(), false, null))
            .Returns(Task.CompletedTask);

        var result = await _controller.Register(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        _signInManager.Verify(m => m.SignInAsync(It.IsAny<IdentityUser>(), false, null), Times.Once);
    }

    [Fact]
    public async Task Register_Post_ValidButCreateFailed_AddsModelErrorsAndReturnsView()
    {
        var model = new RegisterViewModel { Email = "a@b.com", Password = "weak" };
        var errors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password too short" },
            new IdentityError { Code = "Required", Description = "Required field missing" }
        };
        _userManager.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        var result = await _controller.Register(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.Equal(2, _controller.ModelState[string.Empty]!.Errors.Count);
        _signInManager.Verify(m => m.SignInAsync(It.IsAny<IdentityUser>(), It.IsAny<bool>(), null), Times.Never);
    }

    [Fact]
    public async Task Register_Post_InvalidModelState_ReturnsViewWithoutCallingManagers()
    {
        _controller.ModelState.AddModelError("Email", "required");
        var model = new RegisterViewModel();

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _userManager.Verify(m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Login_Get_ReturnsViewWithReturnUrlInViewData()
    {
        var view = Assert.IsType<ViewResult>(_controller.Login("/somewhere"));
        Assert.Equal("/somewhere", view.ViewData["ReturnUrl"]);
    }

    [Fact]
    public async Task Login_Post_ValidAndSuccess_LocalRedirectsToReturnUrl()
    {
        var model = new LoginViewModel { Email = "a@b.com", Password = "P!" };
        _signInManager.Setup(m => m.PasswordSignInAsync(model.Email, model.Password, false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await _controller.Login(model, returnUrl: "/destination");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/destination", redirect.Url);
    }

    [Fact]
    public async Task Login_Post_ValidAndSuccessNoReturnUrl_LocalRedirectsToRoot()
    {
        var model = new LoginViewModel { Email = "a@b.com", Password = "P!" };
        _signInManager.Setup(m => m.PasswordSignInAsync(model.Email, model.Password, false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await _controller.Login(model, returnUrl: null);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task Login_Post_ValidButFailed_AddsModelErrorAndReturnsView()
    {
        var model = new LoginViewModel { Email = "a@b.com", Password = "bad" };
        _signInManager.Setup(m => m.PasswordSignInAsync(model.Email, model.Password, false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await _controller.Login(model, returnUrl: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.True(_controller.ModelState.ErrorCount > 0);
    }

    [Fact]
    public async Task Login_Post_InvalidModelState_ReturnsViewWithoutCallingSignInManager()
    {
        _controller.ModelState.AddModelError("Email", "required");
        var model = new LoginViewModel();

        var result = await _controller.Login(model, returnUrl: null);

        Assert.IsType<ViewResult>(result);
        _signInManager.Verify(
            m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Logout_SignsOutAndRedirectsToHome()
    {
        _signInManager.Setup(m => m.SignOutAsync()).Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        _signInManager.Verify(m => m.SignOutAsync(), Times.Once);
    }
}
