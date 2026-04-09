using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.Services;
using Bookshelf.ViewModels;

namespace Bookshelf.Controllers;

[Authorize]
public class AuthorsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuthorsController(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // GET: Authors
    public async Task<IActionResult> Index()
    {
        var userId = _currentUser.UserId!;

        var authors = await _context.Authors
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return View(authors);
    }

    // GET: Authors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var userId = _currentUser.UserId!;

        var author = await _context.Authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (author == null) return NotFound();

        return View(author);
    }

    // GET: Authors/Create
    public IActionResult Create()
    {
        return View(new AuthorFormViewModel());
    }

    // POST: Authors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuthorFormViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var author = new Author
            {
                Name = viewModel.Name,
                UserId = _currentUser.UserId!
            };
            _context.Add(author);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Authors/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var userId = _currentUser.UserId!;

        var author = await _context.Authors
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (author == null) return NotFound();

        var viewModel = new AuthorFormViewModel
        {
            Id = author.Id,
            Name = author.Name
        };
        return View(viewModel);
    }

    // POST: Authors/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AuthorFormViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var userId = _currentUser.UserId!;

        if (ModelState.IsValid)
        {
            var author = await _context.Authors
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (author == null) return NotFound();

            author.Name = viewModel.Name;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Authors.AnyAsync(a => a.Id == id && a.UserId == userId))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Authors/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var userId = _currentUser.UserId!;

        var author = await _context.Authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (author == null) return NotFound();

        return View(author);
    }

    // POST: Authors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _currentUser.UserId!;

        var author = await _context.Authors
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (author != null)
        {
            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
