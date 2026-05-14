using Bookshelf.Data;
using Bookshelf.Repositories;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Bookshelf.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace Bookshelf.Tests.Repositories;

public class AuthorRepositoryTests : IDisposable
{
    private const string CurrentUserId = RepositoryTestContext.DefaultUserId;
    private const string OtherUserId = "other-user-id";

    private readonly ApplicationDbContext _context = RepositoryTestContext.CreateDbContext();

    public void Dispose() => _context.Dispose();

    private AuthorRepository BuildRepository() =>
        new(_context, RepositoryTestContext.AccessorFor(CurrentUserId));

    private static ModelStateDictionary NewModelState() => new();

    [Fact]
    public void Ctor_NullHttpContext_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new AuthorRepository(_context, RepositoryTestContext.AccessorWithoutHttpContext()));
    }

    [Fact]
    public void Ctor_NoUserIdClaim_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new AuthorRepository(_context, RepositoryTestContext.AccessorWithoutUserClaim()));
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCurrentUserAuthors()
    {
        _context.Authors.Add(new AuthorBuilder().WithName("Mine").WithUserId(CurrentUserId).Build());
        _context.Authors.Add(new AuthorBuilder().WithName("Theirs").WithUserId(OtherUserId).Build());
        await _context.SaveChangesAsync();

        var result = await BuildRepository().ListAsync();

        Assert.Single(result);
        Assert.Equal("Mine", result[0].Name);
    }

    [Fact]
    public async Task FindAsync_OwnedAuthor_ReturnsAuthor()
    {
        var author = new AuthorBuilder().WithName("Mine").WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindAsync(author.Id);

        Assert.NotNull(result);
        Assert.Equal("Mine", result!.Name);
    }

    [Fact]
    public async Task FindAsync_OtherUserAuthor_ReturnsNull()
    {
        var author = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindAsync(author.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindWithBooksAsync_OwnedAuthor_LoadsBooks()
    {
        var author = new AuthorBuilder().WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        _context.Books.Add(new BookBuilder().WithAuthorId(author.Id).Build());
        _context.Books.Add(new BookBuilder().WithAuthorId(author.Id).WithTitle("Other").Build());
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindWithBooksAsync(author.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Books.Count);
    }

    [Fact]
    public async Task FindWithBooksAsync_OtherUserAuthor_ReturnsNull()
    {
        var author = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindWithBooksAsync(author.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_PersistsAuthorScopedToCurrentUser()
    {
        var result = await BuildRepository().CreateAsync(new AuthorFormViewModel { Name = "Ursula" }, NewModelState());

        Assert.Equal(RepositoryResult.Success, result);
        var saved = Assert.Single(_context.Authors);
        Assert.Equal("Ursula", saved.Name);
        Assert.Equal(CurrentUserId, saved.UserId);
    }

    [Fact]
    public async Task UpdateAsync_AuthorNotFound_ReturnsNotFound()
    {
        var result = await BuildRepository().UpdateAsync(id: 999, new AuthorFormViewModel { Name = "X" }, NewModelState());

        Assert.Equal(RepositoryResult.NotFound, result);
    }

    [Fact]
    public async Task UpdateAsync_AuthorOwnedByOtherUser_ReturnsNotFound()
    {
        var author = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().UpdateAsync(author.Id, new AuthorFormViewModel { Name = "X" }, NewModelState());

        Assert.Equal(RepositoryResult.NotFound, result);
    }

    [Fact]
    public async Task UpdateAsync_HappyPath_PersistsRename()
    {
        var author = new AuthorBuilder().WithName("Old").WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().UpdateAsync(author.Id, new AuthorFormViewModel { Name = "New" }, NewModelState());

        Assert.Equal(RepositoryResult.Success, result);
        Assert.Equal("New", (await _context.Authors.FindAsync(author.Id))!.Name);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrencyException_AuthorRemoved_ReturnsNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        using var setupContext = RepositoryTestContext.CreateDbContext(dbName);
        var author = new AuthorBuilder().WithName("Old").WithUserId(CurrentUserId).Build();
        setupContext.Authors.Add(author);
        await setupContext.SaveChangesAsync();

        using var throwing = RepositoryTestContext.CreateDbContext<ThrowingApplicationDbContext>(
            dbName,
            factory: (options, storage) => new ThrowingApplicationDbContext(options, storage));
        var repository = new AuthorRepository(throwing, RepositoryTestContext.AccessorFor(CurrentUserId));

        throwing.ShouldThrowOnSave = true;
        throwing.OnBeforeThrow = async () =>
        {
            using var sideContext = RepositoryTestContext.CreateDbContext(dbName);
            var existing = await sideContext.Authors.FindAsync(author.Id);
            sideContext.Authors.Remove(existing!);
            await sideContext.SaveChangesAsync();
        };

        var result = await repository.UpdateAsync(author.Id, new AuthorFormViewModel { Name = "New" }, NewModelState());

        Assert.Equal(RepositoryResult.NotFound, result);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrencyException_AuthorStillExists_Rethrows()
    {
        var dbName = Guid.NewGuid().ToString();
        using var setupContext = RepositoryTestContext.CreateDbContext(dbName);
        var author = new AuthorBuilder().WithName("Old").WithUserId(CurrentUserId).Build();
        setupContext.Authors.Add(author);
        await setupContext.SaveChangesAsync();

        using var throwing = RepositoryTestContext.CreateDbContext<ThrowingApplicationDbContext>(
            dbName,
            factory: (options, storage) => new ThrowingApplicationDbContext(options, storage));
        var repository = new AuthorRepository(throwing, RepositoryTestContext.AccessorFor(CurrentUserId));

        throwing.ShouldThrowOnSave = true;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => repository.UpdateAsync(author.Id, new AuthorFormViewModel { Name = "New" }, NewModelState()));
    }

    [Fact]
    public async Task Remove_MarksAuthorForDeletion()
    {
        var author = new AuthorBuilder().WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        var repository = BuildRepository();

        repository.Remove(author);
        await repository.SaveAsync();

        Assert.Empty(_context.Authors);
    }
}
