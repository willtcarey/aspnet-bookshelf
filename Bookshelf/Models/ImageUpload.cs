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
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private static readonly ImageFormat JpgFormat = new("jpg", ".jpg", "image/jpeg");
    private static readonly ImageFormat PngFormat = new("png", ".png", "image/png");
    private static readonly ImageFormat WebpFormat = new("webp", ".webp", "image/webp");
    private static readonly Dictionary<string, ImageFormat> Formats = new(StringComparer.OrdinalIgnoreCase)
    {
        [JpgFormat.Name] = JpgFormat,
        ["jpeg"] = JpgFormat,
        [PngFormat.Name] = PngFormat,
        [WebpFormat.Name] = WebpFormat
    };

    private readonly IFileStorage _fileStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly UploadStoragePaths _paths;

    public ImageUpload(
        IFileStorage fileStorage,
        IImageProcessor imageProcessor,
        UploadStoragePaths paths)
    {
        _fileStorage = fileStorage;
        _imageProcessor = imageProcessor;
        _paths = paths;
    }

    public async Task<UploadResult> SaveAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
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
        string format = "webp")
    {
        var normalizedPath = _paths.NormalizeStoredPath(path);
        if (normalizedPath is null)
        {
            return new ImageNotFoundResult();
        }

        if (!IsValidDimension(width) || !IsValidDimension(height))
        {
            return new ImageErrorResult($"Width and height must be between 1 and {MaxResizeDimension}.");
        }

        if (!width.HasValue && !height.HasValue)
        {
            return await GetOriginalAsync(normalizedPath);
        }

        var imageFormat = ResolveFormat(format);
        return imageFormat is null
            ? new ImageErrorResult("Unsupported image format.")
            : await GetResizedAsync(normalizedPath, width, height, imageFormat);
    }

    private async Task<ImageResult> GetOriginalAsync(string sourcePath)
    {
        var stream = await _fileStorage.GetAsync(sourcePath);
        return stream is null
            ? new ImageNotFoundResult()
            : new ImageStreamResult(stream, GetContentTypeFromPath(sourcePath));
    }

    private async Task<ImageResult> GetResizedAsync(
        string normalizedPath,
        int? width,
        int? height,
        ImageFormat format)
    {
        var cachePath = _paths.BuildCachePath(normalizedPath, width, height, format.Extension);
        if (File.Exists(cachePath))
        {
            return new ImageFileResult(cachePath, format.ContentType);
        }

        await using var sourceStream = await _fileStorage.GetAsync(normalizedPath);
        if (sourceStream is null)
        {
            return new ImageNotFoundResult();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

        var resizeWidth = width ?? MaxResizeDimension;
        var resizeHeight = height ?? MaxResizeDimension;

        await using var resizedStream = await _imageProcessor.ResizeAsync(
            sourceStream, resizeWidth, resizeHeight, format.Name);

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

        return new ImageFileResult(cachePath, format.ContentType);
    }

    private static bool IsValidDimension(int? value)
    {
        return !value.HasValue || (value.Value >= 1 && value.Value <= MaxResizeDimension);
    }

    private static ImageFormat? ResolveFormat(string? format)
    {
        return string.IsNullOrWhiteSpace(format)
            ? WebpFormat
            : (Formats.TryGetValue(format.Trim(), out var imageFormat) ? imageFormat : null);
    }

    private static string GetContentTypeFromPath(string path)
    {
        return ContentTypeProvider.TryGetContentType(path, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private sealed record ImageFormat(string Name, string Extension, string ContentType);
}
