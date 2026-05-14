using System.Text;
using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;

namespace Bookshelf.Tests.Services;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _root;
    private readonly UploadStoragePaths _paths;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bookshelf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _paths = TestUploadPaths.Create(_root);
        _storage = new LocalFileStorage(_paths);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_ValidStream_WritesFileToUploadRoot()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var result = await _storage.SaveAsync(stream, "cover.png", "image/png");

        Assert.StartsWith(_paths.UploadRequestPath + "/", result);
        Assert.Single(Directory.GetFiles(_paths.UploadRootPath));
    }

    [Fact]
    public async Task SaveAsync_SeekableStream_RewindsBeforeCopy()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
        stream.Position = stream.Length;

        var result = await _storage.SaveAsync(stream, "x.png", "image/png");

        var fullPath = Path.Combine(_paths.UploadRootPath, Path.GetFileName(result));
        Assert.Equal("payload", await File.ReadAllTextAsync(fullPath));
    }

    [Fact]
    public async Task GetAsync_FileExists_ReturnsReadableStream()
    {
        var fileName = "abc.png";
        await File.WriteAllTextAsync(Path.Combine(_paths.UploadRootPath, fileName), "content");

        await using var stream = await _storage.GetAsync($"/uploads/{fileName}");

        Assert.NotNull(stream);
    }

    [Fact]
    public async Task GetAsync_FileMissing_ReturnsNull()
    {
        var stream = await _storage.GetAsync("/uploads/missing.png");

        Assert.Null(stream);
    }

    [Fact]
    public async Task DeleteAsync_InvalidPath_IsNoOp()
    {
        await _storage.DeleteAsync("not-an-uploads-path");
    }

    [Fact]
    public async Task DeleteAsync_UploadExists_DeletesUpload()
    {
        var uploadPath = Path.Combine(_paths.UploadRootPath, "cover.png");
        await File.WriteAllTextAsync(uploadPath, "x");

        await _storage.DeleteAsync("/uploads/cover.png");

        Assert.False(File.Exists(uploadPath));
    }

    [Fact]
    public async Task DeleteAsync_UploadMissing_DoesNotThrow()
    {
        await _storage.DeleteAsync("/uploads/never-existed.png");
    }

    [Fact]
    public async Task DeleteAsync_CacheVariantExists_DeletesVariant()
    {
        var variantDir = Path.Combine(_paths.CacheRootPath, "100x200");
        Directory.CreateDirectory(variantDir);
        var variantPath = Path.Combine(variantDir, "cover.webp");
        await File.WriteAllTextAsync(variantPath, "x");

        await _storage.DeleteAsync("/uploads/cover.png");

        Assert.False(File.Exists(variantPath));
    }

    [Fact]
    public async Task DeleteAsync_CacheVariantMissing_DoesNotThrow()
    {
        var variantDir = Path.Combine(_paths.CacheRootPath, "100x200");
        Directory.CreateDirectory(variantDir);

        await _storage.DeleteAsync("/uploads/cover.png");
    }

    [Fact]
    public void GetUrl_ValidPath_ReturnsNormalizedPath()
    {
        Assert.Equal("/uploads/cover.png", _storage.GetUrl("/uploads/cover.png"));
    }

    [Fact]
    public void GetUrl_InvalidPath_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, _storage.GetUrl("../etc/passwd"));
    }

    [Fact]
    public void ResolveExtension_FilenameHasExtension_ReturnsLowercaseExtension()
    {
        Assert.Equal(".webp", LocalFileStorage.ResolveExtension("cover.WEBP", "image/png"));
    }

    [Fact]
    public void ResolveExtension_NoFilenameExtension_FallsBackToContentType()
    {
        Assert.Equal(".jpg", LocalFileStorage.ResolveExtension("no-extension", "image/jpeg"));
    }

    [Fact]
    public void ResolveExtension_NoFilenameExtension_PngContentType_ReturnsPng()
    {
        Assert.Equal(".png", LocalFileStorage.ResolveExtension("no-extension", "image/png"));
    }

    [Fact]
    public void ResolveExtension_NoFilenameExtension_GifContentType_ReturnsGif()
    {
        Assert.Equal(".gif", LocalFileStorage.ResolveExtension("no-extension", "image/gif"));
    }

    [Fact]
    public void ResolveExtension_NoFilenameExtension_WebpContentType_ReturnsWebp()
    {
        Assert.Equal(".webp", LocalFileStorage.ResolveExtension("no-extension", "image/webp"));
    }

    [Fact]
    public void ResolveExtension_NoFilenameExtension_UnknownContentType_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, LocalFileStorage.ResolveExtension("no-extension", "application/octet-stream"));
    }
}
