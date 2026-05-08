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
    private readonly HangfireDashboardAuthorizationFilter _filter = new();

    [Fact]
    public void Authorize_AnonymousUser_ReturnsFalse()
    {
        var context = BuildContext(BuildAnonymous());

        Assert.False(_filter.Authorize(context));
    }

    [Fact]
    public void Authorize_AuthenticatedNonAdmin_ReturnsFalse()
    {
        var context = BuildContext(BuildAuthenticated(roles: Array.Empty<string>()));

        Assert.False(_filter.Authorize(context));
    }

    [Fact]
    public void Authorize_AuthenticatedAdmin_ReturnsTrue()
    {
        var context = BuildContext(BuildAuthenticated(roles: new[] { RoleNames.Admin }));

        Assert.True(_filter.Authorize(context));
    }

    [Fact]
    public void Authorize_AuthenticatedWithDifferentRole_ReturnsFalse()
    {
        var context = BuildContext(BuildAuthenticated(roles: new[] { "Editor" }));

        Assert.False(_filter.Authorize(context));
    }

    private static DashboardContext BuildContext(ClaimsPrincipal user)
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
