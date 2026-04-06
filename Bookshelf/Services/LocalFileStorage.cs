namespace Bookshelf.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _uploadRootPath;
    private readonly string _uploadRequestPath;

    public LocalFileStorage(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        var configuredUploadPath = configuration["FileStorage:UploadsPath"];
        var uploadPath = string.IsNullOrWhiteSpace(configuredUploadPath)
            ? "uploads"
            : configuredUploadPath.Trim().Trim('/', '\\');

        _uploadRequestPath = $"/{uploadPath.Replace('\\', '/')}";
        _uploadRootPath = Path.Combine(webRootPath, uploadPath.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(_uploadRootPath);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType)
    {
        var extension = ResolveExtension(fileName, contentType);
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_uploadRootPath, uniqueFileName);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await using var output = File.Create(fullPath);
        await stream.CopyToAsync(output);

        return $"{_uploadRequestPath}/{uniqueFileName}";
    }

    public Task<Stream?> GetAsync(string path)
    {
        var fullPath = ResolveFullPath(path);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.CompletedTask;
        }

        var fullPath = ResolveFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public string GetUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.StartsWith('/') ? path : $"/{path.TrimStart('/')}";
    }

    private string ResolveFullPath(string path)
    {
        var fileName = Path.GetFileName(path.TrimStart('/'));
        return Path.Combine(_uploadRootPath, fileName);
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
