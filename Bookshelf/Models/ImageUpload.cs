using Bookshelf.Services;
using Microsoft.AspNetCore.StaticFiles;

namespace Bookshelf.Models;

public abstract record ImageResult;
public record ImageStreamResult(Stream Stream, string ContentType) : ImageResult;
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
    private static readonly Dictionary<string, ImageFormat> Formats = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jpg"] = ImageFormat.Jpg,
        ["jpeg"] = ImageFormat.Jpg,
        ["png"] = ImageFormat.Png,
        ["webp"] = ImageFormat.Webp
    };

    private readonly IFileStorage _fileStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly ImageStorage _imageStorage;

    public ImageUpload(
        IFileStorage fileStorage,
        IImageProcessor imageProcessor,
        ImageStorage imageStorage)
    {
        _fileStorage = fileStorage;
        _imageProcessor = imageProcessor;
        _imageStorage = imageStorage;
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
        string? key,
        int? width,
        int? height,
        string format = "webp")
    {
        var imageKey = ImageUploadKey.Parse(key);
        if (imageKey is null)
        {
            return new ImageNotFoundResult();
        }

        if (!IsValidDimension(width) || !IsValidDimension(height))
        {
            return new ImageErrorResult($"Width and height must be between 1 and {MaxResizeDimension}.");
        }

        var sourcePath = _imageStorage.BuildStoredPath(imageKey);
        if (!width.HasValue && !height.HasValue)
        {
            return await GetOriginalAsync(sourcePath);
        }

        var imageFormat = ResolveFormat(format);
        return imageFormat is null
            ? new ImageErrorResult("Unsupported image format.")
            : await GetResizedAsync(sourcePath, width, height, imageFormat.Value);
    }

    public string? BuildUrl(string path, int? width = null, int? height = null, string? format = null)
    {
        return _imageStorage.BuildUrl(path, width, height, format);
    }

    private async Task<ImageResult> GetOriginalAsync(string sourcePath)
    {
        var stream = await _fileStorage.GetAsync(sourcePath);
        return stream is null
            ? new ImageNotFoundResult()
            : new ImageStreamResult(stream, GetContentTypeFromPath(sourcePath));
    }

    private async Task<ImageResult> GetResizedAsync(
        string sourcePath,
        int? width,
        int? height,
        ImageFormat format)
    {
        await using var sourceStream = await _fileStorage.GetAsync(sourcePath);
        if (sourceStream is null)
        {
            return new ImageNotFoundResult();
        }

        var resizeWidth = width ?? MaxResizeDimension;
        var resizeHeight = height ?? MaxResizeDimension;

        var resizedStream = await _imageProcessor.ResizeAsync(
            sourceStream, resizeWidth, resizeHeight, GetProcessorFormat(format));

        return new ImageStreamResult(resizedStream, GetContentType(format));
    }

    private static bool IsValidDimension(int? value)
    {
        return !value.HasValue || (value.Value >= 1 && value.Value <= MaxResizeDimension);
    }

    private static ImageFormat? ResolveFormat(string? format)
    {
        return string.IsNullOrWhiteSpace(format)
            ? ImageFormat.Webp
            : (Formats.TryGetValue(format.Trim(), out var imageFormat) ? imageFormat : null);
    }

    private static string GetProcessorFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Jpg => "jpg",
            ImageFormat.Png => "png",
            ImageFormat.Webp => "webp",
            _ => throw new InvalidOperationException("Unsupported image format.")
        };
    }

    private static string GetContentType(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Jpg => "image/jpeg",
            ImageFormat.Png => "image/png",
            ImageFormat.Webp => "image/webp",
            _ => throw new InvalidOperationException("Unsupported image format.")
        };
    }

    private static string GetContentTypeFromPath(string path)
    {
        return ContentTypeProvider.TryGetContentType(path, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private enum ImageFormat
    {
        Jpg,
        Png,
        Webp
    }
}
