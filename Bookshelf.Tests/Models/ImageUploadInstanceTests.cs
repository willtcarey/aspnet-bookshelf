using System.Text;
using Bookshelf.Models;
using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Bookshelf.Tests.Models;

public sealed class ImageUploadInstanceTests : IDisposable
{
    private const string ValidPngKey = "11111111111111111111111111111111.png";
    private const string StoredPngPath = "/uploads/11111111111111111111111111111111.png";
    private const string ValidUnknownKey = "22222222222222222222222222222222.zzzzzz";
    private const string StoredUnknownPath = "/uploads/22222222222222222222222222222222.zzzzzz";

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
    public async Task GetAsyncNullDimensionsReadsOriginalImage()
    {
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("source"));
        _storage.Setup(s => s.GetAsync(StoredPngPath)).ReturnsAsync(sourceStream);

        var result = await _upload.GetAsync(ValidPngKey, null, null);

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Same(sourceStream, streamResult.Stream);
        Assert.Equal("image/png", streamResult.ContentType);
    }

    [Fact]
    public async Task GetAsyncNullDimensionsMissingOriginalReturnsNotFound()
    {
        _storage.Setup(s => s.GetAsync(StoredPngPath)).ReturnsAsync((Stream?)null);

        var result = await _upload.GetAsync(ValidPngKey, null, null);

        Assert.IsType<ImageNotFoundResult>(result);
    }

    [Fact]
    public async Task GetAsyncNullDimensionsUnknownExtensionUsesOctetStreamContentType()
    {
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("source"));
        _storage.Setup(s => s.GetAsync(StoredUnknownPath)).ReturnsAsync(sourceStream);

        var result = await _upload.GetAsync(ValidUnknownKey, null, null);

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Equal("application/octet-stream", streamResult.ContentType);
    }

    [Fact]
    public async Task GetAsyncWidthInvalidReturnsError()
    {
        var result = await _upload.GetAsync(ValidPngKey, 0, null);

        Assert.IsType<ImageErrorResult>(result);
    }

    [Fact]
    public async Task GetAsyncHeightInvalidReturnsError()
    {
        var result = await _upload.GetAsync(ValidPngKey, null, 4001);

        Assert.IsType<ImageErrorResult>(result);
    }

    [Fact]
    public async Task GetAsyncUnsupportedFormatReturnsError()
    {
        var result = await _upload.GetAsync(ValidPngKey, 100, null, format: "tiff");

        var errorResult = Assert.IsType<ImageErrorResult>(result);
        Assert.Equal("Unsupported image format.", errorResult.Message);
    }

    [Fact]
    public async Task GetAsyncValidResizeReturnsWebpStream()
    {
        var resizedStream = SetupResize(StoredPngPath, 100, 200, "webp");

        var result = await _upload.GetAsync(ValidPngKey, 100, 200, format: "webp");

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Same(resizedStream, streamResult.Stream);
        Assert.Equal("image/webp", streamResult.ContentType);
    }

    [Fact]
    public async Task GetAsyncJpegFormatAliasUsesJpgProcessorFormat()
    {
        SetupResize(StoredPngPath, 100, 200, "jpg");

        var result = await _upload.GetAsync(ValidPngKey, 100, 200, format: "jpeg");

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Equal("image/jpeg", streamResult.ContentType);
        _processor.Verify(p => p.ResizeAsync(It.IsAny<Stream>(), 100, 200, "jpg"), Times.Once);
    }

    [Fact]
    public async Task GetAsyncPngFormatUsesPngContentType()
    {
        SetupResize(StoredPngPath, 100, 200, "png");

        var result = await _upload.GetAsync(ValidPngKey, 100, 200, format: "png");

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Equal("image/png", streamResult.ContentType);
    }

    [Fact]
    public async Task GetAsyncBlankFormatDefaultsToWebp()
    {
        SetupResize(StoredPngPath, 100, 200, "webp");

        var result = await _upload.GetAsync(ValidPngKey, 100, 200, format: "  ");

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Equal("image/webp", streamResult.ContentType);
        _processor.Verify(p => p.ResizeAsync(It.IsAny<Stream>(), 100, 200, "webp"), Times.Once);
    }

    [Fact]
    public async Task GetAsyncResizeSourceMissingReturnsNotFound()
    {
        _storage.Setup(s => s.GetAsync(StoredPngPath)).ReturnsAsync((Stream?)null);

        var result = await _upload.GetAsync(ValidPngKey, 100, 200, format: "webp");

        Assert.IsType<ImageNotFoundResult>(result);
        _processor.Verify(p => p.ResizeAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAsyncResizeWidthNullDefaultsToMaxResizeDimension()
    {
        SetupResize(StoredPngPath, 4000, 200, "webp");

        await _upload.GetAsync(ValidPngKey, null, 200, format: "webp");

        _processor.Verify(p => p.ResizeAsync(It.IsAny<Stream>(), 4000, 200, "webp"), Times.Once);
    }

    [Fact]
    public async Task GetAsyncResizeHeightNullDefaultsToMaxResizeDimension()
    {
        SetupResize(StoredPngPath, 100, 4000, "webp");

        await _upload.GetAsync(ValidPngKey, 100, null, format: "webp");

        _processor.Verify(p => p.ResizeAsync(It.IsAny<Stream>(), 100, 4000, "webp"), Times.Once);
    }

    [Fact]
    public void BuildUrlDelegatesToImageStorage()
    {
        var url = _upload.BuildUrl(StoredPngPath, 100, 200, "webp");

        Assert.Equal($"/images/{ValidPngKey}?w=100&h=200&format=webp", url);
    }

    private MemoryStream SetupResize(string storedPath, int width, int height, string format)
    {
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("source"));
        var resizedStream = new MemoryStream(Encoding.UTF8.GetBytes("resized"));
        _storage.Setup(s => s.GetAsync(storedPath)).ReturnsAsync(sourceStream);
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), width, height, format))
            .ReturnsAsync(resizedStream);
        return resizedStream;
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
