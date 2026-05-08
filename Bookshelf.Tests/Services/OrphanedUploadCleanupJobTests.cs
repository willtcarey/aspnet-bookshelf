using Bookshelf.Data;
using Bookshelf.Services;
using Bookshelf.Tests.Builders;
using Bookshelf.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Bookshelf.Tests.Services;

public class OrphanedUploadCleanupJobTests : IDisposable
{
    private readonly string _root;
    private readonly UploadStoragePaths _paths;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IFileStorage> _storage;

    public OrphanedUploadCleanupJobTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bookshelf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _paths = TestUploadPaths.Create(_root);
        _storage = new Mock<IFileStorage>();
        _dbContext = BuildDbContext();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_WhenUploadRootMissing_ReturnsZeroCounts()
    {
        Directory.Delete(_paths.UploadRootPath, recursive: true);
        var job = BuildJob();

        var result = await job.RunAsync();

        Assert.Equal(0, result.ScannedCount);
        Assert.Equal(0, result.SkippedRecentCount);
        Assert.Equal(0, result.DeletedCount);
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_DeletesOrphanedFileOlderThanGracePeriod()
    {
        var orphanPath = Path.Combine(_paths.UploadRootPath, "orphan.png");
        await File.WriteAllTextAsync(orphanPath, "x");
        File.SetLastWriteTimeUtc(orphanPath, DateTime.UtcNow.AddHours(-2));
        var job = BuildJob();

        var result = await job.RunAsync();

        Assert.Equal(1, result.ScannedCount);
        Assert.Equal(0, result.SkippedRecentCount);
        Assert.Equal(1, result.DeletedCount);
        _storage.Verify(s => s.DeleteAsync($"{_paths.UploadRequestPath}/orphan.png"), Times.Once);
    }

    [Fact]
    public async Task RunAsync_SkipsRecentFiles()
    {
        var recentPath = Path.Combine(_paths.UploadRootPath, "recent.png");
        await File.WriteAllTextAsync(recentPath, "x");
        File.SetLastWriteTimeUtc(recentPath, DateTime.UtcNow);
        var job = BuildJob();

        var result = await job.RunAsync();

        Assert.Equal(1, result.ScannedCount);
        Assert.Equal(1, result.SkippedRecentCount);
        Assert.Equal(0, result.DeletedCount);
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_DoesNotDeleteFilesReferencedByABook()
    {
        _dbContext.Books.Add(new BookBuilder()
            .WithId(1)
            .WithCoverImagePath($"{_paths.UploadRequestPath}/in-use.png")
            .Build());
        await _dbContext.SaveChangesAsync();
        var inUsePath = Path.Combine(_paths.UploadRootPath, "in-use.png");
        await File.WriteAllTextAsync(inUsePath, "x");
        File.SetLastWriteTimeUtc(inUsePath, DateTime.UtcNow.AddHours(-2));
        var job = BuildJob();

        var result = await job.RunAsync();

        Assert.Equal(1, result.ScannedCount);
        Assert.Equal(0, result.DeletedCount);
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_HonorsConfiguredGracePeriod()
    {
        var nearMissPath = Path.Combine(_paths.UploadRootPath, "near.png");
        await File.WriteAllTextAsync(nearMissPath, "x");
        File.SetLastWriteTimeUtc(nearMissPath, DateTime.UtcNow.AddMinutes(-10));
        var job = BuildJob(gracePeriodMinutes: 5);

        var result = await job.RunAsync();

        Assert.Equal(1, result.DeletedCount);
    }

    [Fact]
    public async Task RunAsync_FallsBackToDefaultGracePeriodWhenConfigZeroOrNegative()
    {
        var path = Path.Combine(_paths.UploadRootPath, "thirtymin.png");
        await File.WriteAllTextAsync(path, "x");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-30));
        var job = BuildJob(gracePeriodMinutes: -5);

        var result = await job.RunAsync();

        Assert.Equal(1, result.SkippedRecentCount);
        Assert.Equal(0, result.DeletedCount);
    }

    private OrphanedUploadCleanupJob BuildJob(int? gracePeriodMinutes = null)
    {
        var configValues = new Dictionary<string, string?>();
        if (gracePeriodMinutes.HasValue)
        {
            configValues["FileStorage:CleanupGracePeriodMinutes"] = gracePeriodMinutes.Value.ToString();
        }
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new OrphanedUploadCleanupJob(
            _dbContext,
            _storage.Object,
            _paths,
            configuration,
            NullLogger<OrphanedUploadCleanupJob>.Instance);
    }

    private ApplicationDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"orphaned-upload-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options, _storage.Object);
    }
}
