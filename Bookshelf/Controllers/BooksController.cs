using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Models;
using Bookshelf.ViewModels;

namespace Bookshelf.Controllers;

[Authorize]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;

    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Books
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var books = await _context.Books
            .Include(b => b.Author)
            .ToListAsync();

        return View(books);
    }

    // GET: Books/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var book = await _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id);

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
        if (ModelState.IsValid)
        {
            var book = new Book
            {
                Title = viewModel.Title,
                Isbn = viewModel.Isbn,
                Year = viewModel.Year,
                AuthorId = viewModel.AuthorId
            };

            _context.Add(book);
            _context.AttachFile(book, nameof(Book.CoverImagePath), viewModel.CoverImage);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        viewModel.Authors = await BuildAuthorsSelectListAsync(viewModel.AuthorId);
        return View(viewModel);
    }

    // GET: Books/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var book = await _context.Books.FindAsync(id);
        if (book == null) return NotFound();

        var viewModel = new BookFormViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Isbn = book.Isbn,
            Year = book.Year,
            AuthorId = book.AuthorId,
            ExistingCoverImagePath = book.CoverImagePath,
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

        var book = await _context.Books.FindAsync(id);
        if (book == null) return NotFound();

        if (ModelState.IsValid)
        {
            book.Title = viewModel.Title;
            book.Isbn = viewModel.Isbn;
            book.Year = viewModel.Year;
            book.AuthorId = viewModel.AuthorId;

            _context.AttachFile(book, nameof(Book.CoverImagePath), viewModel.CoverImage);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        viewModel.ExistingCoverImagePath = book.CoverImagePath;
        viewModel.Authors = await BuildAuthorsSelectListAsync(viewModel.AuthorId);
        return View(viewModel);
    }

    // GET: Books/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var book = await _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null) return NotFound();

        return View(book);
    }

    // POST: Books/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<SelectList> BuildAuthorsSelectListAsync(int? selectedAuthorId = null)
    {
        var authors = await _context.Authors
            .OrderBy(author => author.Name)
            .ToListAsync();

        return new SelectList(authors, "Id", "Name", selectedAuthorId);
    }
}
