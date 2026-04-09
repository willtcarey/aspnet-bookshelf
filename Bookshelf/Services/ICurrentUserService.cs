namespace Bookshelf.Services;

/// <summary>
/// Provides access to the currently authenticated user's identity.
/// Abstracts away <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> and
/// Identity's <c>UserManager</c> so controllers and services don't have to poke at
/// <c>HttpContext.User</c> directly.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Returns the current user's id, or <c>null</c> if there is no authenticated user.
    /// Callers inside <c>[Authorize]</c>-gated controllers can safely assert non-null.
    /// </summary>
    string? UserId { get; }
}
