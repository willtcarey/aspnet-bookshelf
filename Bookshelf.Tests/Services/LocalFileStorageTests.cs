using System.Text;
using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;

namespace Bookshelf.Tests.Services;

public sealed class LocalFileStorageTests : IDisposable
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
    public async Task SaveAsyncValidStreamWritesFileToUploadRoot()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var result = await _storage.SaveAsync(stream, "cover.png", "image/png");

        Assert.StartsWith(_paths.UploadRequestPath + "/", result, StringComparison.Ordinal);
        Assert.Single(Directory.GetFiles(_paths.UploadRootPath));
    }

    [Fact]
    public async Task SaveAsyncSeekableStreamRewindsBeforeCopy()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
        stream.Position = stream.Length;

        var result = await _storage.SaveAsync(stream, "x.png", "image/png");

        var fullPath = Path.Combine(_paths.UploadRootPath, Path.GetFileName(result));
        Assert.Equal("payload", await File.ReadAllTextAsync(fullPath));
    }

    [Fact]
    public async Task GetAsyncFileExistsReturnsReadableStream()
    {
        var fileName = "abc.png";
        await File.WriteAllTextAsync(Path.Combine(_paths.UploadRootPath, fileName), "content");

        await using var stream = await _storage.GetAsync($"/uploads/{fileName}");

        Assert.NotNull(stream);
    }

    [Fact]
    public async Task GetAsyncFileMissingReturnsNull()
    {
        var stream = await _storage.GetAsync("/uploads/missing.png");

        Assert.Null(stream);
    }

    [Fact]
    public async Task DeleteAsyncInvalidPathIsNoOp()
    {
        await _storage.DeleteAsync("not-an-uploads-path");
    }

    [Fact]
    public async Task DeleteAsyncUploadExistsDeletesUpload()
    {
        var uploadPath = Path.Combine(_paths.UploadRootPath, "cover.png");
        await File.WriteAllTextAsync(uploadPath, "x");

        await _storage.DeleteAsync("/uploads/cover.png");

        Assert.False(File.Exists(uploadPath));
    }

    [Fact]
    public async Task DeleteAsyncUploadMissingDoesNotThrow()
    {
        await _storage.DeleteAsync("/uploads/never-existed.png");
    }

    [Fact]
    public async Task DeleteAsyncCacheVariantExistsDeletesVariant()
    {
        var variantDir = Path.Combine(_paths.CacheRootPath, "100x200");
        Directory.CreateDirectory(variantDir);
        var variantPath = Path.Combine(variantDir, "cover.webp");
        await File.WriteAllTextAsync(variantPath, "x");

        await _storage.DeleteAsync("/uploads/cover.png");

        Assert.False(File.Exists(variantPath));
    }

    [Fact]
    public async Task DeleteAsyncCacheVariantMissingDoesNotThrow()
    {
        var variantDir = Path.Combine(_paths.CacheRootPath, "100x200");
        Directory.CreateDirectory(variantDir);

        await _storage.DeleteAsync("/uploads/cover.png");
    }

    [Fact]
    public void GetUrlValidPathReturnsNormalizedPath()
    {
        Assert.Equal("/uploads/cover.png", _storage.GetUrl("/uploads/cover.png"));
    }

    [Fact]
    public void GetUrlInvalidPathReturnsEmptyString()
    {
        Assert.Equal(string.Empty, _storage.GetUrl("../etc/passwd"));
    }

    [Fact]
    public void ResolveExtensionFilenameHasExtensionReturnsLowercaseExtension()
    {
        Assert.Equal(".webp", LocalFileStorage.ResolveExtension("cover.WEBP", "image/png"));
    }

    [Fact]
    public void ResolveExtensionNoFilenameExtensionFallsBackToContentType()
    {
        Assert.Equal(".jpg", LocalFileStorage.ResolveExtension("no-extension", "image/jpeg"));
    }

    [Fact]
    public void ResolveExtensionNoFilenameExtensionPngContentTypeReturnsPng()
    {
        Assert.Equal(".png", LocalFileStorage.ResolveExtension("no-extension", "image/png"));
    }

    [Fact]
    public void ResolveExtensionNoFilenameExtensionGifContentTypeReturnsGif()
    {
        Assert.Equal(".gif", LocalFileStorage.ResolveExtension("no-extension", "image/gif"));
    }

    [Fact]
    public void ResolveExtensionNoFilenameExtensionWebpContentTypeReturnsWebp()
    {
        Assert.Equal(".webp", LocalFileStorage.ResolveExtension("no-extension", "image/webp"));
    }

    [Fact]
    public void ResolveExtensionNoFilenameExtensionUnknownContentTypeReturnsEmpty()
    {
        Assert.Equal(string.Empty, LocalFileStorage.ResolveExtension("no-extension", "application/octet-stream"));
    }
}
