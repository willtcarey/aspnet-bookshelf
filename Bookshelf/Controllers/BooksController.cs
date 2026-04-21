using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookshelf.Models;
using Bookshelf.Repositories;
using Bookshelf.ViewModels;

namespace Bookshelf.Controllers;

[Authorize]
public class BooksController : Controller
{
    private readonly BookRepository _books;

    public BooksController(BookRepository books)
    {
        _books = books;
    }

    // GET: Books
    public async Task<IActionResult> Index()
    {
        var books = await _books.ListAsync();
        return View(books);
    }

    // GET: Books/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var book = await _books.FindWithAuthorAsync(id.Value);
        if (book == null) return NotFound();

        return View(book);
    }

    // GET: Books/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = new BookFormViewModel
        {
            Authors = await _books.BuildAuthorsSelectListAsync()
        };

        return View(viewModel);
    }

    // POST: Books/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookFormViewModel viewModel)
    {
        // Defense in depth: never trust the AuthorId from the form. Even though the
        // dropdown only renders this user's authors, a hand-crafted POST could submit
        // any id. Reject anything that isn't owned by the current user.
        if (!await _books.IsOwnedAuthorAsync(viewModel.AuthorId))
        {
            ModelState.AddModelError(nameof(viewModel.AuthorId), "Invalid author selection.");
        }

        if (ModelState.IsValid)
        {
            var book = new Book();
            ApplyFormData(book, viewModel);
            _books.Add(book);
            await _books.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        viewModel.Authors = await _books.BuildAuthorsSelectListAsync(viewModel.AuthorId);
        return View(viewModel);
    }

    // GET: Books/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var book = await _books.FindAsync(id.Value);
        if (book == null) return NotFound();

        var viewModel = new BookFormViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Isbn = book.Isbn,
            Year = book.Year,
            AuthorId = book.AuthorId,
            CoverImagePath = book.CoverImagePath,
            Authors = await _books.BuildAuthorsSelectListAsync(book.AuthorId)
        };

        return View(viewModel);
    }

    // POST: Books/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookFormViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var book = await _books.FindAsync(id);
        if (book == null) return NotFound();

        // Defense in depth: same as Create -- reject AuthorIds that don't belong
        // to the current user, even though the dropdown wouldn't offer them.
        if (!await _books.IsOwnedAuthorAsync(viewModel.AuthorId))
        {
            ModelState.AddModelError(nameof(viewModel.AuthorId), "Invalid author selection.");
        }

        if (ModelState.IsValid)
        {
            ApplyFormData(book, viewModel);
            await _books.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        viewModel.Authors = await _books.BuildAuthorsSelectListAsync(viewModel.AuthorId);
        return View(viewModel);
    }

    // GET: Books/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var book = await _books.FindWithAuthorAsync(id.Value);
        if (book == null) return NotFound();

        return View(book);
    }

    // POST: Books/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await _books.FindAsync(id);
        if (book != null)
        {
            _books.Remove(book);
            await _books.SaveAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private static void ApplyFormData(Book book, BookFormViewModel viewModel)
    {
        book.Title = viewModel.Title;
        book.Isbn = viewModel.Isbn;
        book.Year = viewModel.Year;
        book.AuthorId = viewModel.AuthorId;
        book.CoverImagePath = viewModel.CoverImagePath;
    }
}
