using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookshelf.Extensions;
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
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var authors = await _authors.ListAsync();
        return View(authors);
    }

    // GET: Authors/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var author = await _authors.FindWithBooksAsync(id.Value);
        return author == null ? NotFound() : View(author);
    }

    // GET: Authors/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new AuthorFormViewModel());
    }

    // POST: Authors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuthorFormViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        if (ModelState.IsValid)
        {
            var result = await _authors.CreateAsync(viewModel);
            ModelState.AddRepositoryErrors(result);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Authors/Edit/5
    [HttpGet]
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
        ArgumentNullException.ThrowIfNull(viewModel);

        if (id != viewModel.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var result = await _authors.UpdateAsync(id, viewModel);
            ModelState.AddRepositoryErrors(result);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            if (result.IsNotFound) return NotFound();
        }

        return View(viewModel);
    }

    // GET: Authors/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var author = await _authors.FindWithBooksAsync(id.Value);
        return author == null ? NotFound() : View(author);
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
