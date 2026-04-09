using Microsoft.AspNetCore.Identity;

namespace Bookshelf.Services;

/// <summary>
/// Default <see cref="ICurrentUserService"/> implementation that resolves the
/// current user id from <see cref="IHttpContextAccessor"/> via Identity's
/// <see cref="UserManager{TUser}.GetUserId"/>.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<IdentityUser> _userManager;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<IdentityUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public string? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user is null ? null : _userManager.GetUserId(user);
        }
    }
}
