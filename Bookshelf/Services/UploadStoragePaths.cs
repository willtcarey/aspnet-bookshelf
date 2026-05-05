namespace Bookshelf.Services;

public class UploadStoragePaths
{
    public string UploadsPath { get; }
    public string UploadRequestPath { get; }
    public string UploadRootPath { get; }
    public string CacheRootPath { get; }

    public UploadStoragePaths(IWebHostEnvironment environment, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        UploadsPath = NormalizeUploadsPath(configuration["FileStorage:UploadsPath"]);
        UploadRequestPath = $"/{UploadsPath}";
        UploadRootPath = Path.Combine(webRootPath, UploadsPath.Replace('/', Path.DirectorySeparatorChar));
        CacheRootPath = Path.Combine(UploadRootPath, ".cache");

        Directory.CreateDirectory(UploadRootPath);
    }

    public string? NormalizeStoredPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalizedPath = Uri.UnescapeDataString(path)
            .Replace('\\', '/')
            .Trim();

        if (normalizedPath.Contains('\0'))
        {
            return null;
        }

        normalizedPath = normalizedPath.Trim('/');
        var prefix = $"{UploadsPath}/";
        if (!normalizedPath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var fileName = normalizedPath[prefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName is "." or ".."
            || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            return null;
        }

        return $"/{UploadsPath}/{fileName}";
    }

    public string ResolveUploadAbsolutePath(string path)
    {
        var normalizedPath = NormalizeStoredPath(path)
            ?? throw new InvalidOperationException($"Invalid upload path '{path}'.");

        return Path.Combine(UploadRootPath, Path.GetFileName(normalizedPath));
    }

    public string BuildCachePath(string normalizedPath, int? width, int? height, string extension)
    {
        var variantFolder = $"{width?.ToString() ?? "auto"}x{height?.ToString() ?? "auto"}";
        var fileName = Path.GetFileNameWithoutExtension(normalizedPath);

        return Path.Combine(CacheRootPath, variantFolder, $"{fileName}{extension}");
    }

    public IEnumerable<string> EnumerateCacheVariantPaths(string path)
    {
        var normalizedPath = NormalizeStoredPath(path);
        if (normalizedPath is null || !Directory.Exists(CacheRootPath))
        {
            yield break;
        }

        var fileName = Path.GetFileNameWithoutExtension(normalizedPath);
        foreach (var variantDirectory in Directory.EnumerateDirectories(CacheRootPath))
        {
            foreach (var variantPath in Directory.EnumerateFiles(variantDirectory, $"{fileName}.*", SearchOption.TopDirectoryOnly))
            {
                yield return variantPath;
            }
        }
    }

    private static string NormalizeUploadsPath(string? configuredUploadPath)
    {
        var uploadPath = string.IsNullOrWhiteSpace(configuredUploadPath)
            ? "uploads"
            : configuredUploadPath.Trim();

        var normalizedUploadPath = uploadPath
            .Replace('\\', '/')
            .Trim('/');

        return string.IsNullOrWhiteSpace(normalizedUploadPath)
            ? "uploads"
            : normalizedUploadPath;
    }
}
