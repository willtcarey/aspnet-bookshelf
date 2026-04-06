using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.ViewModels;

namespace Bookshelf.Controllers;

[Authorize]
public class AuthorsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuthorsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Authors
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var authors = await _context.Authors.ToListAsync();
        return View(authors);
    }

    // GET: Authors/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var author = await _context.Authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id);

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
            var author = new Author { Name = viewModel.Name };
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

        var author = await _context.Authors.FindAsync(id);
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

        if (ModelState.IsValid)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null) return NotFound();

            author.Name = viewModel.Name;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Authors.AnyAsync(a => a.Id == id))
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

        var author = await _context.Authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (author == null) return NotFound();

        return View(author);
    }

    // POST: Authors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var author = await _context.Authors.FindAsync(id);
        if (author != null)
        {
            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
