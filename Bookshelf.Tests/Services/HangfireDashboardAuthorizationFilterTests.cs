using System.Security.Claims;
using Bookshelf.Security;
using Bookshelf.Services;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Bookshelf.Tests.Services;

public class HangfireDashboardAuthorizationFilterTests
{
    private static readonly string[] EditorRoles = ["Editor"];
    private static readonly string[] AdminRoles = [RoleNames.Admin];

    private readonly HangfireDashboardAuthorizationFilter _filter = new();

    [Fact]
    public void AuthorizePrincipalWithoutIdentityReturnsFalse()
    {
        var context = BuildContext(new ClaimsPrincipal());

        Assert.False(_filter.Authorize(context));
    }

    [Fact]
    public void AuthorizeAnonymousUserReturnsFalse()
    {
        var context = BuildContext(BuildAnonymous());

        Assert.False(_filter.Authorize(context));
    }

    [Fact]
    public void AuthorizeAuthenticatedNonAdminReturnsFalse()
    {
        var context = BuildContext(BuildAuthenticated(roles: EditorRoles));

        Assert.False(_filter.Authorize(context));
    }

    [Fact]
    public void AuthorizeAuthenticatedAdminReturnsTrue()
    {
        var context = BuildContext(BuildAuthenticated(roles: AdminRoles));

        Assert.True(_filter.Authorize(context));
    }

    private static AspNetCoreDashboardContext BuildContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user,
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        return new AspNetCoreDashboardContext(
            Mock.Of<JobStorage>(),
            new DashboardOptions(),
            httpContext);
    }

    private static ClaimsPrincipal BuildAnonymous()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    private static ClaimsPrincipal BuildAuthenticated(IEnumerable<string> roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "ian@brandnewbox.com") };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
    }
}
