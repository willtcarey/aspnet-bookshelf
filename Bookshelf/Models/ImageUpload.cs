using Bookshelf.Services;
using Microsoft.AspNetCore.StaticFiles;

namespace Bookshelf.Models;

public abstract record ImageResult;
public record ImageStreamResult(Stream Stream, string ContentType) : ImageResult;
public record ImageFileResult(string FilePath, string ContentType) : ImageResult;
public record ImageNotFoundResult() : ImageResult;
public record ImageErrorResult(string Message) : ImageResult;

public record UploadResult(bool IsSuccess, string? Path, string? Error)
{
    public static UploadResult Success(string path) => new(true, path, null);
    public static UploadResult Failure(string error) => new(false, null, error);
}

public class ImageUpload
{
    private const int MaxResizeDimension = 4000;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private const string DefaultFormat = "webp";
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly IFileStorage _fileStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly string[] _uploadsPathSegments;
    private readonly string _uploadsRootPath;
    private readonly string _cacheRootPath;

    public ImageUpload(
        IFileStorage fileStorage,
        IImageProcessor imageProcessor,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _fileStorage = fileStorage;
        _imageProcessor = imageProcessor;

        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        var configuredUploadPath = configuration["FileStorage:UploadsPath"];
        var uploadPath = string.IsNullOrWhiteSpace(configuredUploadPath)
            ? "uploads"
            : configuredUploadPath.Trim().Trim('/', '\\');

        _uploadsPathSegments = uploadPath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        _uploadsRootPath = Path.Combine(webRootPath, Path.Combine(_uploadsPathSegments));
        _cacheRootPath = Path.Combine(_uploadsRootPath, ".cache");
    }

    public async Task<UploadResult> SaveAsync(IFormFile? file)
    {
        if (file is not { Length: > 0 })
        {
            return UploadResult.Failure("No file provided.");
        }

        if (file.Length > MaxFileSize)
        {
            return UploadResult.Failure("File size must not exceed 10 MB.");
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return UploadResult.Failure("Only image files are accepted.");
        }

        await using var stream = file.OpenReadStream();
        var path = await _fileStorage.SaveAsync(stream, file.FileName, file.ContentType);

        return UploadResult.Success(path);
    }

    public async Task<ImageResult> GetAsync(
        string? path,
        int? width,
        int? height,
        string format = DefaultFormat)
    {
        var normalizedPath = NormalizePath(path);
        if (normalizedPath is null)
        {
            return new ImageNotFoundResult();
        }

        if (!IsValidDimension(width) || !IsValidDimension(height))
        {
            return new ImageErrorResult($"Width and height must be between 1 and {MaxResizeDimension}.");
        }

        var sourceFilePath = BuildSourceFilePath(normalizedPath);
        if (!File.Exists(sourceFilePath))
        {
            return new ImageNotFoundResult();
        }

        var sourcePath = $"/{normalizedPath}";
        if (!width.HasValue && !height.HasValue)
        {
            return await GetOriginalAsync(sourcePath);
        }

        var normalizedFormat = NormalizeFormat(format);
        if (normalizedFormat is null)
        {
            return new ImageErrorResult("Unsupported image format.");
        }

        return await GetResizedAsync(normalizedPath, sourcePath, width, height, normalizedFormat);
    }

    private async Task<ImageResult> GetOriginalAsync(string sourcePath)
    {
        var stream = await _fileStorage.GetAsync(sourcePath);
        if (stream is null)
        {
            return new ImageNotFoundResult();
        }

        return new ImageStreamResult(stream, GetContentTypeFromPath(sourcePath));
    }

    private async Task<ImageResult> GetResizedAsync(
        string normalizedPath,
        string sourcePath,
        int? width,
        int? height,
        string format)
    {
        var cachePath = BuildCachePath(normalizedPath, width, height, format);
        if (File.Exists(cachePath))
        {
            return new ImageFileResult(cachePath, GetContentTypeForFormat(format));
        }

        await using var sourceStream = await _fileStorage.GetAsync(sourcePath);
        if (sourceStream is null)
        {
            return new ImageNotFoundResult();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

        var resizeWidth = width ?? MaxResizeDimension;
        var resizeHeight = height ?? MaxResizeDimension;

        await using var resizedStream = await _imageProcessor.ResizeAsync(
            sourceStream, resizeWidth, resizeHeight, format);

        var temporaryPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var output = File.Create(temporaryPath))
            {
                if (resizedStream.CanSeek)
                {
                    resizedStream.Position = 0;
                }

                await resizedStream.CopyToAsync(output);
            }

            File.Move(temporaryPath, cachePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return new ImageFileResult(cachePath, GetContentTypeForFormat(format));
    }

    private string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var decodedPath = Uri.UnescapeDataString(path);
        if (decodedPath.Contains('\0'))
        {
            return null;
        }

        var segments = decodedPath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length != _uploadsPathSegments.Length + 1)
        {
            return null;
        }

        for (var i = 0; i < _uploadsPathSegments.Length; i++)
        {
            if (!string.Equals(segments[i], _uploadsPathSegments[i], StringComparison.Ordinal))
            {
                return null;
            }
        }

        var fileName = segments[^1];
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName is "." or ".."
            || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            return null;
        }

        return string.Join('/', _uploadsPathSegments.Append(fileName));
    }

    private static bool IsValidDimension(int? value)
    {
        return !value.HasValue || (value.Value >= 1 && value.Value <= MaxResizeDimension);
    }

    private string BuildSourceFilePath(string normalizedPath)
    {
        return Path.Combine(_uploadsRootPath, Path.GetFileName(normalizedPath));
    }

    private string BuildCachePath(string normalizedPath, int? width, int? height, string format)
    {
        var variantFolder = $"{width?.ToString() ?? "auto"}x{height?.ToString() ?? "auto"}";
        var fileName = Path.GetFileNameWithoutExtension(normalizedPath);
        var extension = GetExtensionForFormat(format);

        return Path.Combine(_cacheRootPath, variantFolder, $"{fileName}{extension}");
    }

    private static string? NormalizeFormat(string? format)
    {
        return format?.Trim().ToLowerInvariant() switch
        {
            null or "" => DefaultFormat,
            "jpg" or "jpeg" => "jpg",
            "png" => "png",
            "webp" => "webp",
            _ => null
        };
    }

    private static string GetExtensionForFormat(string format)
    {
        return format switch
        {
            "jpg" => ".jpg",
            "png" => ".png",
            _ => ".webp"
        };
    }

    private static string GetContentTypeForFormat(string format)
    {
        return format switch
        {
            "jpg" => "image/jpeg",
            "png" => "image/png",
            _ => "image/webp"
        };
    }

    private static string GetContentTypeFromPath(string path)
    {
        return ContentTypeProvider.TryGetContentType(path, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
