using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var books = await _books.ListAsync();
        return View(books);
    }

    // GET: Books/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var book = await _books.FindWithAuthorAsync(id.Value);
        if (book == null) return NotFound();

        return View(book);
    }

    // GET: Books/Create
    [HttpGet]
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
        if (ModelState.IsValid)
        {
            var result = await _books.CreateAsync(viewModel, ModelState);
            if (result == RepositoryResult.Success) return RedirectToAction(nameof(Index));
        }

        viewModel.Authors = await _books.BuildAuthorsSelectListAsync(viewModel.AuthorId);
        return View(viewModel);
    }

    // GET: Books/Edit/5
    [HttpGet]
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

        if (ModelState.IsValid)
        {
            var result = await _books.UpdateAsync(id, viewModel, ModelState);
            if (result == RepositoryResult.Success) return RedirectToAction(nameof(Index));
            if (result == RepositoryResult.NotFound) return NotFound();
        }

        viewModel.Authors = await _books.BuildAuthorsSelectListAsync(viewModel.AuthorId);
        return View(viewModel);
    }

    // GET: Books/Delete/5
    [HttpGet]
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
}
