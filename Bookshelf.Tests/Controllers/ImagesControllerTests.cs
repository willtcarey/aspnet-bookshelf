using System.Text;
using Bookshelf.Controllers;
using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Bookshelf.Tests.Controllers;

public sealed class ImagesControllerTests : IDisposable
{
    private const string ValidKey = "11111111111111111111111111111111.png";
    private const string StoredPath = "/uploads/11111111111111111111111111111111.png";

    private readonly string _root;
    private readonly UploadStoragePaths _paths;
    private readonly ImageStorage _imageStorage;
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IImageProcessor> _processor = new();
    private readonly Bookshelf.Models.ImageUpload _imageUpload;
    private readonly ImagesController _controller;

    public ImagesControllerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bookshelf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _paths = TestUploadPaths.Create(_root);
        _imageStorage = new ImageStorage(_paths);
        _imageUpload = new Bookshelf.Models.ImageUpload(_storage.Object, _processor.Object, _imageStorage);
        _controller = new ImagesController(_imageUpload);
        ControllerTestContext.AttachHttpContext(_controller);
    }

    public void Dispose()
    {
        _controller.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private static FormFile BuildFormFile(byte[]? content = null, string fileName = "x.png", string contentType = "image/png")
    {
        content ??= [1, 2, 3];
        var stream = new MemoryStream(content);
        return new FormFile(stream, baseStreamOffset: 0, length: content.Length, name: "file", fileName: fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task CreateValidFileReturnsOkWithPath()
    {
        var file = BuildFormFile();
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), "x.png", "image/png"))
            .ReturnsAsync("/uploads/saved.png");

        var result = await _controller.Create(file);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task CreateNullFileReturnsBadRequest()
    {
        var result = await _controller.Create(file: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetStreamResultReturnsFileStreamWithCacheHeaders()
    {
        _storage.Setup(s => s.GetAsync(StoredPath))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("source")));

        var result = await _controller.Get(ValidKey, width: null, height: null);

        Assert.IsType<FileStreamResult>(result);
        Assert.Equal(
            "public,max-age=2592000,immutable",
            _controller.Response.Headers[HeaderNames.CacheControl].ToString());
    }

    [Fact]
    public async Task GetResizedStreamReturnsFileStreamWithCacheHeaders()
    {
        _storage.Setup(s => s.GetAsync(StoredPath))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("source")));
        _processor.Setup(p => p.ResizeAsync(It.IsAny<Stream>(), 100, 200, "webp"))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized")));

        var result = await _controller.Get(ValidKey, width: 100, height: 200, format: "webp");

        Assert.IsType<FileStreamResult>(result);
        Assert.Equal(
            "public,max-age=2592000,immutable",
            _controller.Response.Headers[HeaderNames.CacheControl].ToString());
    }

    [Fact]
    public async Task GetInvalidDimensionsReturnsBadRequest()
    {
        var result = await _controller.Get(ValidKey, width: 0, height: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetUnresolvableKeyReturnsNotFound()
    {
        var result = await _controller.Get("not-a-valid-key", width: null, height: null);

        Assert.IsType<NotFoundResult>(result);
    }
}
