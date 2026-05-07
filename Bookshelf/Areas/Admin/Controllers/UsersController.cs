using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bookshelf.Helpers;
using Bookshelf.Areas.Admin.ViewModels;
using Bookshelf.Security;

namespace Bookshelf.Areas.Admin.Controllers;

public class UsersController : AdminController
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private const int PageSize = 20;

    public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Admin/Users
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? sort = null, string? dir = null)
    {
        IQueryable<IdentityUser> query = _userManager.Users;

        query = (sort?.ToUpperInvariant(), dir?.ToUpperInvariant()) switch
        {
            ("EMAIL", "DESC") => query.OrderByDescending(u => u.Email),
            ("EMAIL", _) => query.OrderBy(u => u.Email),
            _ => query.OrderBy(u => u.Email)
        };

        var paginatedUsers = await PaginatedList.CreateAsync(query, page, PageSize, sort, dir);

        var viewModels = new List<AdminUserViewModel>();
        foreach (var user in paginatedUsers)
        {
            viewModels.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                IsAdmin = await _userManager.IsInRoleAsync(user, RoleNames.Admin)
            });
        }

        var result = new PaginatedList<AdminUserViewModel>(
            viewModels, paginatedUsers.TotalCount, paginatedUsers.PageIndex, PageSize, sort, dir);

        return View(result);
    }

    // POST: Admin/Users/ToggleAdmin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == currentUserId)
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            await _userManager.RemoveFromRoleAsync(user, RoleNames.Admin);
        }
        else
        {
            await _userManager.AddToRoleAsync(user, RoleNames.Admin);
        }

        return RedirectToAction(nameof(Index));
    }
}
