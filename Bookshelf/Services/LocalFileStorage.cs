namespace Bookshelf.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly UploadStoragePaths _paths;

    public LocalFileStorage(UploadStoragePaths paths)
    {
        _paths = paths;
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(contentType);

        var extension = ResolveExtension(fileName, contentType);
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_paths.UploadRootPath, uniqueFileName);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await using var output = File.Create(fullPath);
        await stream.CopyToAsync(output);

        return $"{_paths.UploadRequestPath}/{uniqueFileName}";
    }

    public Task<Stream?> GetAsync(string path)
    {
        var fullPath = _paths.ResolveUploadAbsolutePath(path);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string path)
    {
        var normalizedPath = _paths.NormalizeStoredPath(path);
        if (normalizedPath is null)
        {
            return Task.CompletedTask;
        }

        var fullPath = _paths.ResolveUploadAbsolutePath(normalizedPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        foreach (var cacheVariantPath in _paths.EnumerateCacheVariantPaths(normalizedPath))
        {
            if (File.Exists(cacheVariantPath))
            {
                File.Delete(cacheVariantPath);
            }
        }

        return Task.CompletedTask;
    }

    public string GetUrl(string path)
    {
        var normalizedPath = _paths.NormalizeStoredPath(path);
        return normalizedPath ?? string.Empty;
    }

    private static string ResolveExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(Path.GetFileName(fileName));
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.ToLowerInvariant();
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => string.Empty
        };
    }
}
