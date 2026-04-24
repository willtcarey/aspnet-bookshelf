using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookshelf.Data;
using Bookshelf.Helpers;

namespace Bookshelf.Areas.Admin.Controllers;

public abstract class AdminCrudController<TEntity, TFormViewModel> : AdminController
    where TEntity : class, IEntity
    where TFormViewModel : class, IAdminFormViewModel, new()
{
    protected ApplicationDbContext Context { get; }
    private const int PageSize = 20;

    protected AdminCrudController(ApplicationDbContext context)
    {
        Context = context;
    }

    protected abstract DbSet<TEntity> DbSet { get; }
    protected abstract IQueryable<TEntity> GetBaseQuery();
    protected abstract Dictionary<string, Expression<Func<TEntity, object?>>> SortMap { get; }
    protected abstract Expression<Func<TEntity, object?>> DefaultSort { get; }
    protected abstract TFormViewModel MapToViewModel(TEntity entity);
    protected abstract TEntity CreateEntity(TFormViewModel viewModel);
    protected abstract void UpdateEntity(TEntity entity, TFormViewModel viewModel);

    protected virtual Task PopulateFormDataAsync(TFormViewModel viewModel) => Task.CompletedTask;

    // GET: Admin/{Resource}
    public async Task<IActionResult> Index(int page = 1, string? sort = null, string? dir = null)
    {
        var query = GetBaseQuery();
        query = ApplySort(query, sort, dir);
        var paginatedList = await PaginatedList<TEntity>.CreateAsync(query, page, PageSize, sort, dir);
        return View(paginatedList);
    }

    // GET: Admin/{Resource}/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = new TFormViewModel();
        await PopulateFormDataAsync(viewModel);
        return View(viewModel);
    }

    // POST: Admin/{Resource}/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TFormViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var entity = CreateEntity(viewModel);
            DbSet.Add(entity);
            await Context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateFormDataAsync(viewModel);
        return View(viewModel);
    }

    // GET: Admin/{Resource}/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var entity = await DbSet.FindAsync(id);
        if (entity == null) return NotFound();

        var viewModel = MapToViewModel(entity);
        await PopulateFormDataAsync(viewModel);
        return View(viewModel);
    }

    // POST: Admin/{Resource}/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TFormViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var entity = await DbSet.FindAsync(id);
        if (entity == null) return NotFound();

        if (ModelState.IsValid)
        {
            UpdateEntity(entity, viewModel);

            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await DbSet.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PopulateFormDataAsync(viewModel);
        return View(viewModel);
    }

    // GET: Admin/{Resource}/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var entity = await GetBaseQuery().FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null) return NotFound();

        return View(entity);
    }

    // POST: Admin/{Resource}/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity != null)
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private IQueryable<TEntity> ApplySort(IQueryable<TEntity> query, string? sort, string? dir)
    {
        var descending = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
        var sortKey = sort?.ToLowerInvariant();

        if (sortKey != null && SortMap.TryGetValue(sortKey, out var sortExpression))
        {
            return descending
                ? query.OrderByDescending(sortExpression)
                : query.OrderBy(sortExpression);
        }

        return query.OrderBy(DefaultSort);
    }
}
