using Bookshelf.Data;
using Bookshelf.Services;
using Bookshelf.Tests.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Bookshelf.Tests.Data;

public class ApplicationDbContextTests
{
    private static ApplicationDbContext BuildContext(Mock<IFileStorage> storage, IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }
        return new ApplicationDbContext(builder.Options, storage.Object);
    }

    [Fact]
    public async Task SaveChangesAsyncAddedEntityWithPathNoDeletesOnSuccess()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage);
        var author = new AuthorBuilder().Build();
        context.Authors.Add(author);
        await context.SaveChangesAsync(); // need an author so book FK isn't violated

        var book = new BookBuilder().WithAuthorId(author.Id).WithCoverImagePath("/uploads/new.png").Build();
        context.Books.Add(book);

        await context.SaveChangesAsync();

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsyncAddedEntityWithPathDeletesNewPathOnFailure()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage, new FailingSaveInterceptor());
        var book = new BookBuilder().WithCoverImagePath("/uploads/new.png").Build();
        context.Books.Add(book);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        storage.Verify(s => s.DeleteAsync("/uploads/new.png"), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsyncAddedEntityWithBlankPathNoDeleteCalls()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage, new FailingSaveInterceptor());
        var book = new BookBuilder().WithCoverImagePath(null).Build();
        context.Books.Add(book);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsyncModifiedEntityPathChangedDeletesOldOnSuccess()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage);
        var author = new AuthorBuilder().Build();
        context.Authors.Add(author);
        var book = new BookBuilder().WithAuthorId(author.Id).WithCoverImagePath("/uploads/old.png").Build();
        context.Books.Add(book);
        await context.SaveChangesAsync();

        book.CoverImagePath = "/uploads/new.png";
        await context.SaveChangesAsync();

        storage.Verify(s => s.DeleteAsync("/uploads/old.png"), Times.Once);
        storage.Verify(s => s.DeleteAsync("/uploads/new.png"), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsyncModifiedEntityPathChangedDeletesNewOnFailure()
    {
        var storage = new Mock<IFileStorage>();
        var dbName = Guid.NewGuid().ToString();

        var setupOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName).Options;
        await using (var setupContext = new ApplicationDbContext(setupOptions, storage.Object))
        {
            var author = new AuthorBuilder().Build();
            setupContext.Authors.Add(author);
            var book = new BookBuilder().WithAuthorId(author.Id).WithCoverImagePath("/uploads/old.png").Build();
            setupContext.Books.Add(book);
            await setupContext.SaveChangesAsync();
        }
        storage.Invocations.Clear();

        var throwingOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new FailingSaveInterceptor()).Options;
        await using var throwingContext = new ApplicationDbContext(throwingOptions, storage.Object);
        var tracked = (await throwingContext.Books.FirstAsync())!;
        tracked.CoverImagePath = "/uploads/new.png";

        await Assert.ThrowsAsync<DbUpdateException>(() => throwingContext.SaveChangesAsync());

        storage.Verify(s => s.DeleteAsync("/uploads/new.png"), Times.Once);
        storage.Verify(s => s.DeleteAsync("/uploads/old.png"), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsyncModifiedEntityPathUnchangedNoDeleteCalls()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage);
        var author = new AuthorBuilder().Build();
        context.Authors.Add(author);
        var book = new BookBuilder().WithAuthorId(author.Id).WithCoverImagePath("/uploads/cover.png").Build();
        context.Books.Add(book);
        await context.SaveChangesAsync();

        book.Title = "New Title"; // mutate non-attachment property
        await context.SaveChangesAsync();

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsyncModifiedEntityOriginalBlankDeletesOnlyNewOnFailure()
    {
        var storage = new Mock<IFileStorage>();
        var dbName = Guid.NewGuid().ToString();

        var setupOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName).Options;
        await using (var setupContext = new ApplicationDbContext(setupOptions, storage.Object))
        {
            var author = new AuthorBuilder().Build();
            setupContext.Authors.Add(author);
            var book = new BookBuilder().WithAuthorId(author.Id).WithCoverImagePath(null).Build();
            setupContext.Books.Add(book);
            await setupContext.SaveChangesAsync();
        }
        storage.Invocations.Clear();

        var throwingOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new FailingSaveInterceptor()).Options;
        await using var throwingContext = new ApplicationDbContext(throwingOptions, storage.Object);
        var tracked = (await throwingContext.Books.FirstAsync())!;
        tracked.CoverImagePath = "/uploads/new.png";

        await Assert.ThrowsAsync<DbUpdateException>(() => throwingContext.SaveChangesAsync());

        storage.Verify(s => s.DeleteAsync("/uploads/new.png"), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsyncModifiedEntityNewBlankDeletesOnlyOldOnSuccess()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage);
        var author = new AuthorBuilder().Build();
        context.Authors.Add(author);
        var book = new BookBuilder().WithAuthorId(author.Id).WithCoverImagePath("/uploads/old.png").Build();
        context.Books.Add(book);
        await context.SaveChangesAsync();

        book.CoverImagePath = null;
        await context.SaveChangesAsync();

        storage.Verify(s => s.DeleteAsync("/uploads/old.png"), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsyncDeletedEntityWithPathDeletesPathOnSuccess()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage);
        var author = new AuthorBuilder().Build();
        context.Authors.Add(author);
        var book = new BookBuilder().WithAuthorId(author.Id).WithCoverImagePath("/uploads/cover.png").Build();
        context.Books.Add(book);
        await context.SaveChangesAsync();

        context.Books.Remove(book);
        await context.SaveChangesAsync();

        storage.Verify(s => s.DeleteAsync("/uploads/cover.png"), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsyncDeletedEntityWithBlankPathNoDeleteCalls()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage);
        var author = new AuthorBuilder().Build();
        context.Authors.Add(author);
        var book = new BookBuilder().WithAuthorId(author.Id).WithCoverImagePath(null).Build();
        context.Books.Add(book);
        await context.SaveChangesAsync();

        context.Books.Remove(book);
        await context.SaveChangesAsync();

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsyncUnchangedEntityNoDeleteCalls()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage);
        var author = new AuthorBuilder().Build();
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        storage.Invocations.Clear();

        // No state change, just calling SaveChangesAsync.
        await context.SaveChangesAsync();

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsyncEntityWithoutAttachmentPropertiesNoDeleteCalls()
    {
        var storage = new Mock<IFileStorage>();
        await using var context = BuildContext(storage);
        var author = new AuthorBuilder().Build();
        context.Authors.Add(author); // Author has no [FileAttachment] property
        await context.SaveChangesAsync();

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("simulated save failure");
        }
    }
}
