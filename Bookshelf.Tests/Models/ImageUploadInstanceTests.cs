using System.Text;
using Bookshelf.Models;
using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Bookshelf.Tests.Models;

public class ImageUploadInstanceTests : IDisposable
{
    private readonly string _root;
    private readonly UploadStoragePaths _paths;
    private readonly Mock<IFileStorage> _storage;
    private readonly Mock<IImageProcessor> _processor;
    private readonly ImageUpload _upload;

    public ImageUploadInstanceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bookshelf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _paths = TestUploadPaths.Create(_root);
        _storage = new Mock<IFileStorage>();
        _processor = new Mock<IImageProcessor>();
        _upload = new ImageUpload(_storage.Object, _processor.Object, _paths);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_NullFile_ReturnsFailure()
    {
        var result = await _upload.SaveAsync(null);

        Assert.False(result.IsSuccess);
        Assert.Equal("No file provided.", result.Error);
    }

    [Fact]
    public async Task SaveAsync_FileExceedsMaxSize_ReturnsFailure()
    {
        var file = BuildFormFile(content: new byte[11 * 1024 * 1024], contentType: "image/png");

        var result = await _upload.SaveAsync(file);

        Assert.False(result.IsSuccess);
        Assert.Contains("10 MB", result.Error);
    }

    [Fact]
    public async Task SaveAsync_NonImageContentType_ReturnsFailure()
    {
        var file = BuildFormFile(content: new byte[] { 1, 2, 3 }, contentType: "application/pdf");

        var result = await _upload.SaveAsync(file);

        Assert.False(result.IsSuccess);
        Assert.Equal("Only image files are accepted.", result.Error);
    }

    [Fact]
    public async Task SaveAsync_ValidImage_ReturnsSuccessPath()
    {
        var file = BuildFormFile(content: new byte[] { 1, 2, 3 }, fileName: "x.png", contentType: "image/png");
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), "x.png", "image/png"))
            .ReturnsAsync("/uploads/saved.png");

        var result = await _upload.SaveAsync(file);

        Assert.True(result.IsSuccess);
        Assert.Equal("/uploads/saved.png", result.Path);
    }

    [Fact]
    public async Task GetAsync_PathInvalid_ReturnsNotFound()
    {
        var result = await _upload.GetAsync("not-uploads", null, null);

        Assert.IsType<ImageNotFoundResult>(result);
    }

    [Fact]
    public async Task GetAsync_WidthInvalid_ReturnsError()
    {
        var result = await _upload.GetAsync("/uploads/cover.png", 0, null);

        Assert.IsType<ImageErrorResult>(result);
    }

    [Fact]
    public async Task GetAsync_HeightInvalid_ReturnsError()
    {
        var result = await _upload.GetAsync("/uploads/cover.png", null, 4001);

        Assert.IsType<ImageErrorResult>(result);
    }

    [Fact]
    public async Task GetAsync_NoDimensions_DelegatesToOriginal()
    {
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("source"));
        _storage.Setup(s => s.GetAsync("/uploads/cover.png")).ReturnsAsync(sourceStream);

        var result = await _upload.GetAsync("/uploads/cover.png", null, null);

        Assert.IsType<ImageStreamResult>(result);
    }

    [Fact]
    public async Task GetAsync_UnsupportedFormat_ReturnsError()
    {
        var result = await _upload.GetAsync("/uploads/cover.png", 100, null, format: "tiff");

        var errorResult = Assert.IsType<ImageErrorResult>(result);
        Assert.Equal("Unsupported image format.", errorResult.Message);
    }

    [Fact]
    public async Task GetAsync_WidthOnlyValidFormat_DelegatesToResize()
    {
        var cachePath = _paths.BuildCachePath("/uploads/cover.png", 100, null, ".webp");
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        await File.WriteAllTextAsync(cachePath, "cached");

        var result = await _upload.GetAsync("/uploads/cover.png", 100, null, format: "webp");

        Assert.IsType<ImageFileResult>(result);
    }

    [Fact]
    public async Task GetAsync_BothDimensionsValidFormat_DelegatesToResize()
    {
        var cachePath = _paths.BuildCachePath("/uploads/cover.png", 100, 200, ".webp");
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        await File.WriteAllTextAsync(cachePath, "cached");

        var result = await _upload.GetAsync("/uploads/cover.png", 100, 200, format: "webp");

        Assert.IsType<ImageFileResult>(result);
    }

    [Fact]
    public async Task GetOriginalAsync_StorageReturnsStream_ReturnsStreamResult()
    {
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("source"));
        _storage.Setup(s => s.GetAsync("/uploads/cover.png")).ReturnsAsync(sourceStream);

        var result = await _upload.GetOriginalAsync("/uploads/cover.png");

        var streamResult = Assert.IsType<ImageStreamResult>(result);
        Assert.Equal("image/png", streamResult.ContentType);
    }

    [Fact]
    public async Task GetOriginalAsync_StorageReturnsNull_ReturnsNotFound()
    {
        _storage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((Stream?)null);

        var result = await _upload.GetOriginalAsync("/uploads/cover.png");

        Assert.IsType<ImageNotFoundResult>(result);
    }

    [Fact]
    public async Task GetResizedAsync_CacheHit_ReturnsCachedFile()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        var cachePath = _paths.BuildCachePath("/uploads/cover.png", 100, 200, format.Extension);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        await File.WriteAllTextAsync(cachePath, "cached");

        var result = await _upload.GetResizedAsync("/uploads/cover.png", 100, 200, format);

        var fileResult = Assert.IsType<ImageFileResult>(result);
        Assert.Equal(cachePath, fileResult.FilePath);
        _storage.Verify(s => s.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetResizedAsync_CacheMissAndSourceMissing_ReturnsNotFound()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        _storage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((Stream?)null);

        var result = await _upload.GetResizedAsync("/uploads/cover.png", 100, 200, format);

        Assert.IsType<ImageNotFoundResult>(result);
    }

    [Fact]
    public async Task GetResizedAsync_CacheMiss_PersistsResizedFile()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("src"));
        var resizedStream = new MemoryStream(Encoding.UTF8.GetBytes("resized-bytes"));
        _storage.Setup(s => s.GetAsync("/uploads/cover.png")).ReturnsAsync(sourceStream);
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), 100, 200, "webp"))
            .ReturnsAsync(resizedStream);

        var result = await _upload.GetResizedAsync("/uploads/cover.png", 100, 200, format);

        var fileResult = Assert.IsType<ImageFileResult>(result);
        Assert.Equal("resized-bytes", await File.ReadAllTextAsync(fileResult.FilePath));
    }

    [Fact]
    public async Task GetResizedAsync_WidthNull_DefaultsToMaxResizeDimension()
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
    public async Task GetResizedAsync_HeightNull_DefaultsToMaxResizeDimension()
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
    public async Task GetResizedAsync_SeekableResizedStream_RewindsBeforeWriting()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("src"));
        var resizedStream = new MemoryStream(Encoding.UTF8.GetBytes("resized"));
        resizedStream.Position = resizedStream.Length;
        _storage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync(sourceStream);
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(resizedStream);

        var result = await _upload.GetResizedAsync("/uploads/cover.png", 100, 200, format);

        var fileResult = Assert.IsType<ImageFileResult>(result);
        Assert.Equal("resized", await File.ReadAllTextAsync(fileResult.FilePath));
    }

    [Fact]
    public async Task GetResizedAsync_AfterMove_TempFileDoesNotExist()
    {
        var format = ImageUpload.ResolveFormat("webp")!;
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("src"));
        var resizedStream = new MemoryStream(Encoding.UTF8.GetBytes("resized"));
        _storage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync(sourceStream);
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(resizedStream);

        var result = await _upload.GetResizedAsync("/uploads/cover.png", 100, 200, format);

        var fileResult = Assert.IsType<ImageFileResult>(result);
        var tempFiles = Directory.GetFiles(Path.GetDirectoryName(fileResult.FilePath)!, "*.tmp");
        Assert.Empty(tempFiles);
    }

    private static IFormFile BuildFormFile(byte[] content, string fileName = "x.png", string contentType = "image/png")
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, baseStreamOffset: 0, length: content.Length, name: "file", fileName: fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
