using Bookshelf.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookshelf.Services;

public class OrphanedUploadCleanupJob
{
    private static readonly TimeSpan DefaultGracePeriod = TimeSpan.FromHours(1);

    private readonly ApplicationDbContext _dbContext;
    private readonly IFileStorage _fileStorage;
    private readonly UploadStoragePaths _paths;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrphanedUploadCleanupJob> _logger;

    public OrphanedUploadCleanupJob(
        ApplicationDbContext dbContext,
        IFileStorage fileStorage,
        UploadStoragePaths paths,
        IConfiguration configuration,
        ILogger<OrphanedUploadCleanupJob> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _paths = paths;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OrphanedUploadCleanupResult> RunAsync()
    {
        if (!Directory.Exists(_paths.UploadRootPath))
        {
            _logger.LogInformation("Upload cleanup skipped because {UploadRootPath} does not exist.", _paths.UploadRootPath);
            return new OrphanedUploadCleanupResult(0, 0, 0);
        }

        var gracePeriod = ResolveGracePeriod();
        var cutoff = DateTime.UtcNow.Subtract(gracePeriod);

        var referencedPaths = await _dbContext.Books
            .AsNoTracking()
            .Select(book => book.CoverImagePath)
            .Where(path => path != null)
            .ToListAsync();

        var referencedUploads = referencedPaths
            .Select(_paths.NormalizeStoredPath)
            .Where(path => path != null)
            .ToHashSet(StringComparer.Ordinal);

        var scannedCount = 0;
        var skippedRecentCount = 0;
        var deletedCount = 0;

        foreach (var uploadPath in Directory.EnumerateFiles(_paths.UploadRootPath, "*", SearchOption.TopDirectoryOnly))
        {
            scannedCount++;

            var fileInfo = new FileInfo(uploadPath);
            if (fileInfo.LastWriteTimeUtc >= cutoff)
            {
                skippedRecentCount++;
                continue;
            }

            var storedPath = $"{_paths.UploadRequestPath}/{fileInfo.Name}";
            if (referencedUploads.Contains(storedPath))
            {
                continue;
            }

            await _fileStorage.DeleteAsync(storedPath);
            deletedCount++;
        }

        _logger.LogInformation(
            "Upload cleanup completed. Scanned {ScannedCount} files, skipped {SkippedRecentCount} recent files, deleted {DeletedCount} orphaned uploads.",
            scannedCount,
            skippedRecentCount,
            deletedCount);

        return new OrphanedUploadCleanupResult(scannedCount, skippedRecentCount, deletedCount);
    }

    private TimeSpan ResolveGracePeriod()
    {
        var configuredMinutes = _configuration.GetValue<int?>("FileStorage:CleanupGracePeriodMinutes");
        if (configuredMinutes is > 0)
        {
            return TimeSpan.FromMinutes(configuredMinutes.Value);
        }

        return DefaultGracePeriod;
    }
}

public record OrphanedUploadCleanupResult(int ScannedCount, int SkippedRecentCount, int DeletedCount);
