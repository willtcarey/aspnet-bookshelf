using System.Security.Claims;
using Bookshelf.Data;
using Bookshelf.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Bookshelf.Tests.TestSupport;

internal static class RepositoryTestContext
{
    public const string DefaultUserId = "test-user-id";

    public static ApplicationDbContext CreateDbContext(string? databaseName = null)
    {
        return CreateDbContext<ApplicationDbContext>(databaseName, (options, storage) => new ApplicationDbContext(options, storage));
    }

    public static T CreateDbContext<T>(string? databaseName, Func<DbContextOptions<ApplicationDbContext>, IFileStorage, T> factory)
        where T : ApplicationDbContext
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return factory(options, Mock.Of<IFileStorage>());
    }

    public static IHttpContextAccessor AccessorFor(string userId = DefaultUserId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            authenticationType: "Test");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        return accessor.Object;
    }

    public static IHttpContextAccessor AccessorWithoutHttpContext()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        return accessor.Object;
    }

    public static IHttpContextAccessor AccessorWithoutUserClaim()
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        return accessor.Object;
    }
}

internal sealed class ThrowingApplicationDbContext : ApplicationDbContext
{
    public bool ShouldThrowOnSave { get; set; }
    public Func<Task>? OnBeforeThrow { get; set; }

    public ThrowingApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IFileStorage fileStorage)
        : base(options, fileStorage)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldThrowOnSave)
        {
            if (OnBeforeThrow is not null)
            {
                await OnBeforeThrow();
            }
            throw new DbUpdateConcurrencyException("simulated concurrency");
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
