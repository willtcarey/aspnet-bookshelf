using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookshelf.Models;
using Bookshelf.Repositories;
using Bookshelf.ViewModels;

namespace Bookshelf.Controllers;

[Authorize]
public class AuthorsController : Controller
{
    private readonly AuthorRepository _authors;

    public AuthorsController(AuthorRepository authors)
    {
        _authors = authors;
    }

    // GET: Authors
    public async Task<IActionResult> Index()
    {
        var authors = await _authors.ListAsync();
        return View(authors);
    }

    // GET: Authors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var author = await _authors.FindWithBooksAsync(id.Value);
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
            _authors.Add(author);
            await _authors.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Authors/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var author = await _authors.FindAsync(id.Value);
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
            var author = await _authors.FindAsync(id);
            if (author == null) return NotFound();

            author.Name = viewModel.Name;

            try
            {
                await _authors.SaveAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                if (!await _authors.ExistsAsync(id))
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

        var author = await _authors.FindWithBooksAsync(id.Value);
        if (author == null) return NotFound();

        return View(author);
    }

    // POST: Authors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var author = await _authors.FindAsync(id);
        if (author != null)
        {
            _authors.Remove(author);
            await _authors.SaveAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
