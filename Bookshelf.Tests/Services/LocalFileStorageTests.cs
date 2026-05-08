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
    public async Task SaveAsync_WritesStreamToUploadRoot_AndReturnsRequestPath()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var result = await _storage.SaveAsync(stream, "cover.png", "image/png");

        Assert.StartsWith(_paths.UploadRequestPath + "/", result);
        Assert.EndsWith(".png", result);
        var savedFiles = Directory.GetFiles(_paths.UploadRootPath);
        Assert.Single(savedFiles);
    }

    [Fact]
    public async Task SaveAsync_UsesContentTypeWhenFilenameHasNoExtension()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await _storage.SaveAsync(stream, "no-extension", "image/jpeg");

        Assert.EndsWith(".jpg", result);
    }

    [Fact]
    public async Task SaveAsync_PreservesExtensionFromFilename()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await _storage.SaveAsync(stream, "cover.WEBP", "image/png");

        Assert.EndsWith(".webp", result);
    }

    [Fact]
    public async Task SaveAsync_UnknownContentTypeAndNoExtension_ProducesFileWithoutExtension()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await _storage.SaveAsync(stream, "bare", "application/octet-stream");

        var fileName = result[(_paths.UploadRequestPath.Length + 1)..];
        Assert.False(Path.HasExtension(fileName));
    }

    [Fact]
    public async Task SaveAsync_RewindsSeekableStreamBeforeCopying()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
        stream.Position = stream.Length;

        var result = await _storage.SaveAsync(stream, "x.png", "image/png");

        var fullPath = Path.Combine(_paths.UploadRootPath, Path.GetFileName(result));
        Assert.Equal("payload", await File.ReadAllTextAsync(fullPath));
    }

    [Fact]
    public async Task GetAsync_WhenFileExists_ReturnsReadableStream()
    {
        var fileName = "abc.png";
        var fullPath = Path.Combine(_paths.UploadRootPath, fileName);
        await File.WriteAllTextAsync(fullPath, "content");

        await using var stream = await _storage.GetAsync($"/uploads/{fileName}");

        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        Assert.Equal("content", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task GetAsync_WhenFileMissing_ReturnsNull()
    {
        var stream = await _storage.GetAsync("/uploads/missing.png");

        Assert.Null(stream);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUploadAndCacheVariants()
    {
        var uploadPath = Path.Combine(_paths.UploadRootPath, "cover.png");
        var variantDir = Path.Combine(_paths.CacheRootPath, "100x200");
        Directory.CreateDirectory(variantDir);
        var variantPath = Path.Combine(variantDir, "cover.webp");
        await File.WriteAllTextAsync(uploadPath, "x");
        await File.WriteAllTextAsync(variantPath, "x");

        await _storage.DeleteAsync("/uploads/cover.png");

        Assert.False(File.Exists(uploadPath));
        Assert.False(File.Exists(variantPath));
    }

    [Fact]
    public async Task DeleteAsync_InvalidPath_IsNoOp()
    {
        await _storage.DeleteAsync("not-an-uploads-path");
    }

    [Fact]
    public async Task DeleteAsync_UploadAlreadyMissing_DoesNotThrow()
    {
        await _storage.DeleteAsync("/uploads/never-existed.png");
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
}
