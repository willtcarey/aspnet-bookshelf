using Bookshelf.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace Bookshelf.Controllers;

[Route("images")]
public class ImagesController : Controller
{
    private const int MaxResizeDimension = 4000;
    private const string DefaultFormat = "webp";
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly IFileStorage _fileStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly string[] _uploadsPathSegments;
    private readonly string _uploadsRootPath;
    private readonly string _cacheRootPath;

    public ImagesController(
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

    [HttpPost("upload")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        const long maxFileSize = 10 * 1024 * 1024; // 10 MB

        if (file is not { Length: > 0 })
        {
            return BadRequest(new { error = "No file provided." });
        }

        if (file.Length > maxFileSize)
        {
            return BadRequest(new { error = "File size must not exceed 10 MB." });
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only image files are accepted." });
        }

        await using var stream = file.OpenReadStream();
        var path = await _fileStorage.SaveAsync(stream, file.FileName, file.ContentType);

        return Ok(new { path });
    }

    [HttpGet("{*path}")]
    [HttpHead("{*path}")]
    public async Task<IActionResult> Get(
        string? path,
        [FromQuery(Name = "w")] int? width,
        [FromQuery(Name = "h")] int? height,
        [FromQuery] string format = DefaultFormat)
    {
        var normalizedPath = NormalizeRequestedPath(path);
        if (normalizedPath is null)
        {
            return NotFound();
        }

        if (!TryNormalizeDimension(width, out var normalizedWidth)
            || !TryNormalizeDimension(height, out var normalizedHeight))
        {
            return BadRequest($"Width and height must be between 1 and {MaxResizeDimension}.");
        }

        var sourceFilePath = BuildSourceFilePath(normalizedPath);
        if (!System.IO.File.Exists(sourceFilePath))
        {
            return NotFound();
        }

        var sourcePath = $"/{normalizedPath}";
        if (!normalizedWidth.HasValue && !normalizedHeight.HasValue)
        {
            return await ServeOriginalAsync(sourcePath);
        }

        var normalizedFormat = NormalizeFormat(format);
        if (normalizedFormat is null)
        {
            return BadRequest("Unsupported image format.");
        }

        var cachePath = BuildCachePath(normalizedPath, normalizedWidth, normalizedHeight, normalizedFormat);
        if (System.IO.File.Exists(cachePath))
        {
            ApplyCacheHeaders();
            return PhysicalFile(cachePath, GetContentTypeForFormat(normalizedFormat));
        }

        await using var sourceStream = await _fileStorage.GetAsync(sourcePath);
        if (sourceStream is null)
        {
            return NotFound();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

        var resizeWidth = normalizedWidth ?? MaxResizeDimension;
        var resizeHeight = normalizedHeight ?? MaxResizeDimension;

        await using var resizedStream = await _imageProcessor.ResizeAsync(sourceStream, resizeWidth, resizeHeight, normalizedFormat);

        var temporaryPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var output = System.IO.File.Create(temporaryPath))
            {
                if (resizedStream.CanSeek)
                {
                    resizedStream.Position = 0;
                }

                await resizedStream.CopyToAsync(output);
            }

            System.IO.File.Move(temporaryPath, cachePath, overwrite: true);
        }
        finally
        {
            if (System.IO.File.Exists(temporaryPath))
            {
                System.IO.File.Delete(temporaryPath);
            }
        }

        ApplyCacheHeaders();
        return PhysicalFile(cachePath, GetContentTypeForFormat(normalizedFormat));
    }

    private async Task<IActionResult> ServeOriginalAsync(string sourcePath)
    {
        var sourceStream = await _fileStorage.GetAsync(sourcePath);
        if (sourceStream is null)
        {
            return NotFound();
        }

        ApplyCacheHeaders();
        return File(sourceStream, GetContentTypeFromPath(sourcePath));
    }

    private string? NormalizeRequestedPath(string? path)
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

    private static bool TryNormalizeDimension(int? value, out int? normalizedValue)
    {
        normalizedValue = value;

        if (!value.HasValue)
        {
            return true;
        }

        if (value.Value < 1 || value.Value > MaxResizeDimension)
        {
            normalizedValue = null;
            return false;
        }

        return true;
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

    private void ApplyCacheHeaders()
    {
        Response.Headers[HeaderNames.CacheControl] = "public,max-age=2592000,immutable";
    }
}
