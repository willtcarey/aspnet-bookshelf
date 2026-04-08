using Hangfire.Annotations;
using Hangfire.Dashboard;
using Bookshelf.Security;

namespace Bookshelf.Services;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(RoleNames.Admin);
    }
}
