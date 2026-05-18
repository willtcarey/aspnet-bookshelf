using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Bookshelf.Tests.TestSupport;

/// <summary>
/// Mock factories for ASP.NET Identity managers. UserManager / SignInManager
/// / RoleManager are designed to be mocked (their public methods are virtual)
/// but their constructors take a lot of dependencies, so building the mocks
/// directly is verbose. These helpers wrap that boilerplate.
/// </summary>
internal static class IdentityMocks
{
    public static Mock<UserManager<IdentityUser>> UserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object,
            /* optionsAccessor */ null!,
            /* passwordHasher */ null!,
            /* userValidators */ null!,
            /* passwordValidators */ null!,
            /* keyNormalizer */ null!,
            /* errors */ null!,
            /* services */ null!,
            /* logger */ null!);
    }

    public static Mock<SignInManager<IdentityUser>> SignInManager(UserManager<IdentityUser> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
        return new Mock<SignInManager<IdentityUser>>(
            userManager,
            contextAccessor.Object,
            userPrincipalFactory.Object,
            /* optionsAccessor */ null!,
            /* logger */ null!,
            /* schemes */ null!,
            /* confirmation */ null!);
    }

    public static Mock<RoleManager<IdentityRole>> RoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object,
            /* roleValidators */ null!,
            /* keyNormalizer */ null!,
            /* errors */ null!,
            /* logger */ null!);
    }
}
