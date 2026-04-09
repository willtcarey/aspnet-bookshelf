using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.Services;
using Bookshelf.ViewModels;

namespace Bookshelf.Controllers;

[Authorize]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BooksController(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // GET: Books
    public async Task<IActionResult> Index()
    {
        var userId = _currentUser.UserId!;

        var books = await _context.Books
            .Where(b => b.UserId == userId)
            .Include(b => b.Author)
            .ToListAsync();

        return View(books);
    }

    // GET: Books/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var userId = _currentUser.UserId!;

        var book = await _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (book == null) return NotFound();

        return View(book);
    }

    // GET: Books/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = new BookFormViewModel
        {
            Authors = await BuildAuthorsSelectListAsync()
        };

        return View(viewModel);
    }

    // POST: Books/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookFormViewModel viewModel)
    {
        var userId = _currentUser.UserId!;

        // Defense in depth: never trust the AuthorId from the form. Even though the
        // dropdown only renders this user's authors, a hand-crafted POST could submit
        // any id. Reject anything that isn't owned by the current user.
        if (!await IsOwnedAuthorAsync(viewModel.AuthorId, userId))
        {
            ModelState.AddModelError(nameof(viewModel.AuthorId), "Invalid author selection.");
        }

        if (ModelState.IsValid)
        {
            var book = new Book { UserId = userId };
            _context.Add(book);
            await ApplyAndSaveAsync(book, viewModel);
            return RedirectToAction(nameof(Index));
        }

        viewModel.Authors = await BuildAuthorsSelectListAsync(viewModel.AuthorId);
        return View(viewModel);
    }

    // GET: Books/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var userId = _currentUser.UserId!;

        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (book == null) return NotFound();

        var viewModel = new BookFormViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Isbn = book.Isbn,
            Year = book.Year,
            AuthorId = book.AuthorId,
            CoverImagePath = book.CoverImagePath,
            Authors = await BuildAuthorsSelectListAsync(book.AuthorId)
        };

        return View(viewModel);
    }

    // POST: Books/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookFormViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var userId = _currentUser.UserId!;

        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (book == null) return NotFound();

        // Defense in depth: same as Create -- reject AuthorIds that don't belong
        // to the current user, even though the dropdown wouldn't offer them.
        if (!await IsOwnedAuthorAsync(viewModel.AuthorId, userId))
        {
            ModelState.AddModelError(nameof(viewModel.AuthorId), "Invalid author selection.");
        }

        if (ModelState.IsValid)
        {
            await ApplyAndSaveAsync(book, viewModel);
            return RedirectToAction(nameof(Index));
        }

        viewModel.Authors = await BuildAuthorsSelectListAsync(viewModel.AuthorId);
        return View(viewModel);
    }

    // GET: Books/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var userId = _currentUser.UserId!;

        var book = await _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (book == null) return NotFound();

        return View(book);
    }

    // POST: Books/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _currentUser.UserId!;

        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ApplyAndSaveAsync(Book book, BookFormViewModel viewModel)
    {
        book.Title = viewModel.Title;
        book.Isbn = viewModel.Isbn;
        book.Year = viewModel.Year;
        book.AuthorId = viewModel.AuthorId;
        book.CoverImagePath = viewModel.CoverImagePath;

        await _context.SaveChangesAsync();
    }

    // TODO: This builds view-specific presentation data (SelectList) from a DB query.
    // Should this live somewhere else? Every controller that needs an author dropdown
    // would duplicate this. Could move to a shared service, a view component, or a
    // method on the DbContext/repository.
    private async Task<SelectList> BuildAuthorsSelectListAsync(int? selectedAuthorId = null)
    {
        var userId = _currentUser.UserId!;

        var authors = await _context.Authors
            .Where(a => a.UserId == userId)
            .OrderBy(author => author.Name)
            .ToListAsync();

        return new SelectList(authors, "Id", "Name", selectedAuthorId);
    }

    private Task<bool> IsOwnedAuthorAsync(int authorId, string userId)
    {
        return _context.Authors.AnyAsync(a => a.Id == authorId && a.UserId == userId);
    }
}
