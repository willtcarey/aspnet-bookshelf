using System.Text;
using Bookshelf.Models;
using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Bookshelf.Tests.Models;

public sealed class ImageUploadInstanceTests : IDisposable
{
    private const string ValidKey = "11111111111111111111111111111111.png";
    private const string StoredPath = "/uploads/11111111111111111111111111111111.png";

    private readonly string _root;
    private readonly UploadStoragePaths _paths;
    private readonly ImageStorage _imageStorage;
    private readonly Mock<IFileStorage> _storage;
    private readonly Mock<IImageProcessor> _processor;
    private readonly ImageUpload _upload;

    public ImageUploadInstanceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bookshelf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _paths = TestUploadPaths.Create(_root);
        _imageStorage = new ImageStorage(_paths);
        _storage = new Mock<IFileStorage>();
        _processor = new Mock<IImageProcessor>();
        _upload = new ImageUpload(_storage.Object, _processor.Object, _imageStorage);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsyncNullFileReturnsFailure()
    {
        var result = await _upload.SaveAsync(null);

        Assert.False(result.IsSuccess);
        Assert.Equal("No file provided.", result.Error);
    }

    [Fact]
    public async Task SaveAsyncEmptyFileReturnsFailure()
    {
        var file = BuildFormFile(content: Array.Empty<byte>(), contentType: "image/png");

        var result = await _upload.SaveAsync(file);

        Assert.False(result.IsSuccess);
        Assert.Equal("No file provided.", result.Error);
    }

    [Fact]
    public async Task SaveAsyncFileExceedsMaxSizeReturnsFailure()
    {
        var file = BuildFormFile(content: new byte[11 * 1024 * 1024], contentType: "image/png");

        var result = await _upload.SaveAsync(file);

        Assert.False(result.IsSuccess);
        Assert.Contains("10 MB", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsyncNonImageContentTypeReturnsFailure()
    {
        var file = BuildFormFile(content: new byte[] { 1, 2, 3 }, contentType: "application/pdf");

        var result = await _upload.SaveAsync(file);

        Assert.False(result.IsSuccess);
        Assert.Equal("Only image files are accepted.", result.Error);
    }

    [Fact]
    public async Task SaveAsyncValidImageReturnsSuccessPath()
    {
        var file = BuildFormFile(content: new byte[] { 1, 2, 3 }, fileName: "x.png", contentType: "image/png");
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), "x.png", "image/png"))
            .ReturnsAsync("/uploads/saved.png");

        var result = await _upload.SaveAsync(file);

        Assert.True(result.IsSuccess);
        Assert.Equal("/uploads/saved.png", result.Path);
    }

    [Fact]
    public async Task GetAsyncInvalidKeyReturnsNotFound()
    {
        var result = await _upload.GetAsync("has/slash", null, null);

        Assert.IsType<ImageNotFoundResult>(result);
    }

    [Fact]
    public async Task GetAsyncNullKeyReturnsNotFound()
    {
        var result = await _upload.GetAsync(null, null, null);

        Assert.IsType<ImageNotFoundResult>(result);
    }

    [Fact]
    public async Task GetAsyncWidthInvalidReturnsError()
    {
        var result = await _upload.GetAsync(ValidKey, 0, null);

        Assert.IsType<ImageErrorResult>(result);
    }

    [Fact]
    public async Task GetAsyncHeightInvalidReturnsError()
    {
        var result = await _upload.GetAsync(ValidKey, null, 4001);

        Assert.IsType<ImageErrorResult>(result);
    }

    [Fact]
    public async Task GetAsyncNoDimensionsDelegatesToOriginal()
    {
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("source"));
        _storage.Setup(s => s.GetAsync(StoredPath)).ReturnsAsync(sourceStream);

        var result = await _upload.GetAsync(ValidKey, null, null);

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Equal("image/png", streamResult.ContentType);
    }

    [Fact]
    public async Task GetAsyncUnsupportedFormatReturnsError()
    {
        var result = await _upload.GetAsync(ValidKey, 100, null, format: "tiff");

        var errorResult = Assert.IsType<ImageErrorResult>(result);
        Assert.Equal("Unsupported image format.", errorResult.Message);
    }

    [Fact]
    public async Task GetAsyncValidParamsDelegatesToResize()
    {
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("source"));
        var resizedStream = new MemoryStream(Encoding.UTF8.GetBytes("resized"));
        _storage.Setup(s => s.GetAsync(StoredPath)).ReturnsAsync(sourceStream);
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), 100, 200, "webp"))
            .ReturnsAsync(resizedStream);

        var result = await _upload.GetAsync(ValidKey, 100, 200, format: "webp");

        Assert.IsType<ImageStreamResult>(result);
    }

    [Fact]
    public async Task GetOriginalAsyncStorageReturnsStreamReturnsStreamResult()
    {
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("source"));
        _storage.Setup(s => s.GetAsync("/uploads/cover.png")).ReturnsAsync(sourceStream);

        var result = await _upload.GetOriginalAsync("/uploads/cover.png");

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Equal("image/png", streamResult.ContentType);
    }

    [Fact]
    public async Task GetOriginalAsyncStorageReturnsNullReturnsNotFound()
    {
        _storage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((Stream?)null);

        var result = await _upload.GetOriginalAsync("/uploads/cover.png");

        Assert.IsType<ImageNotFoundResult>(result);
    }

    [Fact]
    public async Task GetResizedAsyncSourceMissingReturnsNotFound()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        _storage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((Stream?)null);

        var result = await _upload.GetResizedAsync("/uploads/cover.png", 100, 200, format);

        Assert.IsType<ImageNotFoundResult>(result);
    }

    [Fact]
    public async Task GetResizedAsyncReturnsResizedStream()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("src"));
        var resizedStream = new MemoryStream(Encoding.UTF8.GetBytes("resized-bytes"));
        _storage.Setup(s => s.GetAsync("/uploads/cover.png")).ReturnsAsync(sourceStream);
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), 100, 200, "webp"))
            .ReturnsAsync(resizedStream);

        var result = await _upload.GetResizedAsync("/uploads/cover.png", 100, 200, format);

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Same(resizedStream, streamResult.Stream);
        Assert.Equal("image/webp", streamResult.ContentType);
    }

    [Fact]
    public async Task GetResizedAsyncWidthNullDefaultsToMaxResizeDimension()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("src"));
        var resizedStream = new MemoryStream(Encoding.UTF8.GetBytes("resized"));
        _storage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync(sourceStream);
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), 4000, 200, "webp"))
            .ReturnsAsync(resizedStream)
            .Verifiable();

        await _upload.GetResizedAsync("/uploads/cover.png", null, 200, format);

        _processor.Verify();
    }

    [Fact]
    public async Task GetResizedAsyncHeightNullDefaultsToMaxResizeDimension()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("src"));
        var resizedStream = new MemoryStream(Encoding.UTF8.GetBytes("resized"));
        _storage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync(sourceStream);
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), 100, 4000, "webp"))
            .ReturnsAsync(resizedStream)
            .Verifiable();

        await _upload.GetResizedAsync("/uploads/cover.png", 100, null, format);

        _processor.Verify();
    }

    [Fact]
    public void BuildUrlDelegatesToImageStorage()
    {
        var url = _upload.BuildUrl(StoredPath, 100, 200, "webp");

        Assert.Equal($"/images/{ValidKey}?w=100&h=200&format=webp", url);
    }

    private static FormFile BuildFormFile(byte[] content, string fileName = "x.png", string contentType = "image/png")
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, baseStreamOffset: 0, length: content.Length, name: "file", fileName: fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
