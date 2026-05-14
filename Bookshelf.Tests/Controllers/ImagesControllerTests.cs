using System.Text;
using Bookshelf.Controllers;
using Bookshelf.Models;
using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Bookshelf.Tests.Controllers;

public class ImagesControllerTests : IDisposable
{
    private readonly string _root;
    private readonly UploadStoragePaths _paths;
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IImageProcessor> _processor = new();
    private readonly ImageUpload _imageUpload;
    private readonly ImagesController _controller;

    public ImagesControllerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bookshelf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _paths = TestUploadPaths.Create(_root);
        _imageUpload = new ImageUpload(_storage.Object, _processor.Object, _paths);
        _controller = new ImagesController(_imageUpload);
        ControllerTestContext.AttachHttpContext(_controller);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static IFormFile BuildFormFile(byte[]? content = null, string fileName = "x.png", string contentType = "image/png")
    {
        content ??= new byte[] { 1, 2, 3 };
        var stream = new MemoryStream(content);
        return new FormFile(stream, baseStreamOffset: 0, length: content.Length, name: "file", fileName: fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task Create_ValidFile_ReturnsOkWithPath()
    {
        var file = BuildFormFile();
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), "x.png", "image/png"))
            .ReturnsAsync("/uploads/saved.png");

        var result = await _controller.Create(file);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Create_NullFile_ReturnsBadRequest()
    {
        var result = await _controller.Create(file: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_StreamResult_ReturnsFileStreamWithCacheHeaders()
    {
        await File.WriteAllBytesAsync(Path.Combine(_paths.UploadRootPath, "cover.png"), new byte[] { 1, 2, 3 });
        _storage.Setup(s => s.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("source")));

        var result = await _controller.Get("/uploads/cover.png", width: null, height: null);

        Assert.IsType<FileStreamResult>(result);
        Assert.Equal(
            "public,max-age=2592000,immutable",
            _controller.Response.Headers[HeaderNames.CacheControl].ToString());
    }

    [Fact]
    public async Task Get_FileResult_ReturnsPhysicalFileWithCacheHeaders()
    {
        var cachePath = _paths.BuildCachePath("/uploads/cover.png", 100, 200, ".webp");
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        await File.WriteAllTextAsync(cachePath, "cached");

        var result = await _controller.Get("/uploads/cover.png", width: 100, height: 200, format: "webp");

        var physical = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal(cachePath, physical.FileName);
        Assert.Equal(
            "public,max-age=2592000,immutable",
            _controller.Response.Headers[HeaderNames.CacheControl].ToString());
    }

    [Fact]
    public async Task Get_InvalidDimensions_ReturnsBadRequest()
    {
        var result = await _controller.Get("/uploads/cover.png", width: 0, height: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_UnresolvablePath_ReturnsNotFound()
    {
        var result = await _controller.Get("not-an-uploads-path", width: null, height: null);

        Assert.IsType<NotFoundResult>(result);
    }
}
