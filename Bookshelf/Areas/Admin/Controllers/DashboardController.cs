using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;

namespace Bookshelf.Areas.Admin.Controllers;

public class DashboardController : AdminController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["BookCount"] = await _context.Books.CountAsync();
        ViewData["AuthorCount"] = await _context.Authors.CountAsync();
        ViewData["UserCount"] = await _userManager.Users.CountAsync();
        return View();
    }
}
