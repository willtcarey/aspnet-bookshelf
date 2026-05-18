using Bookshelf.Data;
using Bookshelf.Repositories;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Bookshelf.ViewModels;

namespace Bookshelf.Tests.Repositories;

public sealed class BookRepositoryTests : IDisposable
{
    private const string CurrentUserId = RepositoryTestContext.DefaultUserId;
    private const string OtherUserId = "other-user-id";

    private readonly ApplicationDbContext _context = RepositoryTestContext.CreateDbContext();

    public void Dispose() => _context.Dispose();

    private BookRepository BuildRepository() =>
        new(_context, RepositoryTestContext.AccessorFor(CurrentUserId));

    [Fact]
    public void CtorNullHttpContextThrows()
    {
        Assert.Throws<InvalidOperationException>(
            () => new BookRepository(_context, RepositoryTestContext.AccessorWithoutHttpContext()));
    }

    [Fact]
    public void CtorNoUserIdClaimThrows()
    {
        Assert.Throws<InvalidOperationException>(
            () => new BookRepository(_context, RepositoryTestContext.AccessorWithoutUserClaim()));
    }

    [Fact]
    public async Task ListAsyncReturnsOnlyBooksOwnedByCurrentUserAuthor()
    {
        var mineAuthor = new AuthorBuilder().WithName("Mine").WithUserId(CurrentUserId).Build();
        var theirsAuthor = new AuthorBuilder().WithName("Theirs").WithUserId(OtherUserId).Build();
        _context.Authors.AddRange(mineAuthor, theirsAuthor);
        await _context.SaveChangesAsync();
        _context.Books.Add(new BookBuilder().WithTitle("Mine").WithAuthorId(mineAuthor.Id).Build());
        _context.Books.Add(new BookBuilder().WithTitle("Theirs").WithAuthorId(theirsAuthor.Id).Build());
        await _context.SaveChangesAsync();

        var result = await BuildRepository().ListAsync();

        Assert.Single(result);
        Assert.Equal("Mine", result[0].Title);
        Assert.NotNull(result[0].Author);
    }

    [Fact]
    public async Task FindAsyncOwnedBookReturnsBook()
    {
        var author = new AuthorBuilder().WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        var book = new BookBuilder().WithAuthorId(author.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindAsync(book.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FindAsyncOtherUserBookReturnsNull()
    {
        var author = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        var book = new BookBuilder().WithAuthorId(author.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindAsync(book.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindWithAuthorAsyncOwnedBookLoadsAuthor()
    {
        var author = new AuthorBuilder().WithName("Ursula").WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        var book = new BookBuilder().WithAuthorId(author.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindWithAuthorAsync(book.Id);

        Assert.NotNull(result);
        Assert.Equal("Ursula", result!.Author.Name);
    }

    [Fact]
    public async Task FindWithAuthorAsyncOtherUserBookReturnsNull()
    {
        var author = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        var book = new BookBuilder().WithAuthorId(author.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().FindWithAuthorAsync(book.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsyncAuthorNotOwnedReturnsValidationFailed()
    {
        var foreignAuthor = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(foreignAuthor);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().CreateAsync(
            new BookFormViewModel { Title = "X", AuthorId = foreignAuthor.Id });

        Assert.False(result.Succeeded);
        Assert.True(result.HasValidationErrors);
        Assert.Equal(nameof(BookFormViewModel.AuthorId), result.ValidationErrors[0].Key);
        Assert.Empty(_context.Books);
    }

    [Fact]
    public async Task CreateAsyncAuthorOwnedPersistsBook()
    {
        var author = new AuthorBuilder().WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().CreateAsync(
            new BookFormViewModel { Title = "A Wizard of Earthsea", AuthorId = author.Id, Year = 1968 });

        Assert.True(result.Succeeded);
        var saved = Assert.Single(_context.Books);
        Assert.Equal("A Wizard of Earthsea", saved.Title);
        Assert.Equal(author.Id, saved.AuthorId);
    }

    [Fact]
    public async Task UpdateAsyncBookNotFoundReturnsNotFound()
    {
        var result = await BuildRepository().UpdateAsync(
            id: 999,
            new BookFormViewModel { Title = "X", AuthorId = 1 });

        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task UpdateAsyncBookOwnedByOtherUserReturnsNotFound()
    {
        var foreignAuthor = new AuthorBuilder().WithUserId(OtherUserId).Build();
        _context.Authors.Add(foreignAuthor);
        await _context.SaveChangesAsync();
        var book = new BookBuilder().WithAuthorId(foreignAuthor.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().UpdateAsync(
            book.Id,
            new BookFormViewModel { Title = "X", AuthorId = foreignAuthor.Id });

        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task UpdateAsyncAuthorReassignmentNotOwnedReturnsValidationFailed()
    {
        var mineAuthor = new AuthorBuilder().WithName("Mine").WithUserId(CurrentUserId).Build();
        var foreignAuthor = new AuthorBuilder().WithName("Theirs").WithUserId(OtherUserId).Build();
        _context.Authors.AddRange(mineAuthor, foreignAuthor);
        await _context.SaveChangesAsync();
        var book = new BookBuilder().WithAuthorId(mineAuthor.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().UpdateAsync(
            book.Id,
            new BookFormViewModel { Title = "X", AuthorId = foreignAuthor.Id });

        Assert.False(result.Succeeded);
        Assert.True(result.HasValidationErrors);
    }

    [Fact]
    public async Task UpdateAsyncHappyPathPersistsChanges()
    {
        var author = new AuthorBuilder().WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        var book = new BookBuilder().WithTitle("Old").WithAuthorId(author.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var result = await BuildRepository().UpdateAsync(
            book.Id,
            new BookFormViewModel { Title = "New", AuthorId = author.Id, Year = 2026 });

        Assert.True(result.Succeeded);
        var refreshed = await _context.Books.FindAsync(book.Id);
        Assert.Equal("New", refreshed!.Title);
        Assert.Equal(2026, refreshed.Year);
    }

    [Fact]
    public async Task RemoveMarksBookForDeletion()
    {
        var author = new AuthorBuilder().WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        var book = new BookBuilder().WithAuthorId(author.Id).Build();
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        var repository = BuildRepository();

        repository.Remove(book);
        await repository.SaveAsync();

        Assert.Empty(_context.Books);
    }

    [Fact]
    public async Task BuildAuthorsSelectListAsyncReturnsOnlyCurrentUserAuthorsOrderedByName()
    {
        _context.Authors.Add(new AuthorBuilder().WithName("Charlie").WithUserId(CurrentUserId).Build());
        _context.Authors.Add(new AuthorBuilder().WithName("Alpha").WithUserId(CurrentUserId).Build());
        _context.Authors.Add(new AuthorBuilder().WithName("Foreign").WithUserId(OtherUserId).Build());
        await _context.SaveChangesAsync();

        var list = await BuildRepository().BuildAuthorsSelectListAsync();
        var items = list.ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("Alpha", items[0].Text);
        Assert.Equal("Charlie", items[1].Text);
    }

    [Fact]
    public async Task BuildAuthorsSelectListAsyncWithSelectedIdMarksSelected()
    {
        var author = new AuthorBuilder().WithName("Alpha").WithUserId(CurrentUserId).Build();
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var list = await BuildRepository().BuildAuthorsSelectListAsync(selectedAuthorId: author.Id);
        var item = Assert.Single(list);

        Assert.True(item.Selected);
    }
}
