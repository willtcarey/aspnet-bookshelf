using Bookshelf.Data;
using Bookshelf.Repositories;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Bookshelf.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Bookshelf.Tests.Repositories;

public sealed class AuthorRepositoryTests : IDisposable
{
    private const string CurrentUserId = RepositoryTestContext.DefaultUserId;
    private const string OtherUserId = "other-user-id";

    private readonly ApplicationDbContext _context = RepositoryTestContext.CreateDbContext();

    public void Dispose() => _context.Dispose();

    private AuthorRepository BuildRepository() =>
        new(_context, RepositoryTestContext.AccessorFor(CurrentUserId));

    [Fact]
    public void CtorNullHttpContextThrows()
    {
        Assert.Throws<InvalidOperationException>(
            () => new AuthorRepository(_context, RepositoryTestContext.AccessorWithoutHttpContext()));
    }

    [Fact]
    public void CtorNoUserIdClaimThrows()
    {
        Assert.Throws<InvalidOperationException>(
            () => new AuthorRepository(_context, RepositoryTestContext.AccessorWithoutUserClaim()));
    }

    [Fact]
    public async Task ListAsyncReturnsOnlyCurrentUserAuthors()
    {
        _context.Authors.Add(new AuthorBuilder().WithName("Mine").WithUserId(CurrentUserId).Build());
        _context.Authors.Add(new AuthorBuilder().WithName("Theirs").WithUserId(OtherUserId).Build());
        await _context.SaveChangesAsync();

        var result = await BuildRepository().ListAsync();

        Assert.Single(result);
        Assert.Equal("Mine", result[0].Name);
    }

    [Fact]
    public async Task FindAsyncOwnedAuthorReturnsAuthor()
    {
        var author = new AuthorBuilder().WithName("Mine").WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindAsync(author.Id);

        Assert.NotNull(result);
        Assert.Equal("Mine", result!.Name);
    }

    [Fact]
    public async Task FindAsyncOtherUserAuthorReturnsNull()
    {
        var author = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindAsync(author.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindWithBooksAsyncOwnedAuthorLoadsBooks()
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
    public async Task FindWithBooksAsyncOtherUserAuthorReturnsNull()
    {
        var author = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindWithBooksAsync(author.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsyncPersistsAuthorScopedToCurrentUser()
    {
        var result = await BuildRepository().CreateAsync(new AuthorFormViewModel { Name = "Ursula" });

        Assert.True(result.Succeeded);
        var saved = Assert.Single(_context.Authors);
        Assert.Equal("Ursula", saved.Name);
        Assert.Equal(CurrentUserId, saved.UserId);
    }

    [Fact]
    public async Task CreateAsyncDuplicateNameReturnsValidationFailed()
    {
        _context.Authors.Add(new AuthorBuilder().WithName("Ursula").WithUserId(CurrentUserId).Build());
        await _context.SaveChangesAsync();

        var result = await BuildRepository().CreateAsync(new AuthorFormViewModel { Name = "Ursula" });

        Assert.False(result.Succeeded);
        Assert.True(result.HasValidationErrors);
        Assert.Equal(nameof(AuthorFormViewModel.Name), result.ValidationErrors[0].Key);
    }

    [Fact]
    public async Task UpdateAsyncAuthorNotFoundReturnsNotFound()
    {
        var result = await BuildRepository().UpdateAsync(id: 999, new AuthorFormViewModel { Name = "X" });

        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task UpdateAsyncAuthorOwnedByOtherUserReturnsNotFound()
    {
        var author = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().UpdateAsync(author.Id, new AuthorFormViewModel { Name = "X" });

        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task UpdateAsyncDuplicateNameReturnsValidationFailed()
    {
        _context.Authors.Add(new AuthorBuilder().WithName("Existing").WithUserId(CurrentUserId).Build());
        var author = new AuthorBuilder().WithName("Old").WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().UpdateAsync(author.Id, new AuthorFormViewModel { Name = "Existing" });

        Assert.False(result.Succeeded);
        Assert.True(result.HasValidationErrors);
    }

    [Fact]
    public async Task UpdateAsyncHappyPathPersistsRename()
    {
        var author = new AuthorBuilder().WithName("Old").WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().UpdateAsync(author.Id, new AuthorFormViewModel { Name = "New" });

        Assert.True(result.Succeeded);
        Assert.Equal("New", (await _context.Authors.FindAsync(author.Id))!.Name);
    }

    [Fact]
    public async Task UpdateAsyncConcurrencyExceptionAuthorRemovedReturnsNotFound()
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

        var result = await repository.UpdateAsync(author.Id, new AuthorFormViewModel { Name = "New" });

        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task UpdateAsyncConcurrencyExceptionAuthorStillExistsRethrows()
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
            () => repository.UpdateAsync(author.Id, new AuthorFormViewModel { Name = "New" }));
    }

    [Fact]
    public async Task RemoveMarksAuthorForDeletion()
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
