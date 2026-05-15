using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;

namespace Bookshelf.Tests.Services;

public sealed class UploadStoragePathsTests : IDisposable
{
    private readonly string _root;

    public UploadStoragePathsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bookshelf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void ConstructorUsesWebRootPathWhenSet()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal(Path.Combine(_root, "uploads"), paths.UploadRootPath);
    }

    [Fact]
    public void ConstructorFallsBackToContentRootWwwrootWhenWebRootEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: "", contentRoot: _root);

        Assert.Equal(Path.Combine(_root, "wwwroot", "uploads"), paths.UploadRootPath);
    }

    [Fact]
    public void NormalizeStoredPathBlankInputReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath(null));
    }

    [Fact]
    public void NormalizeStoredPathNullByteInPathReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("uploads/foo\0.png"));
    }

    [Fact]
    public void NormalizeStoredPathNotInUploadsRootReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("other/cover.png"));
    }

    [Fact]
    public void NormalizeStoredPathDotFileNameReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("uploads/.."));
    }

    [Fact]
    public void NormalizeStoredPathNestedPathReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("uploads/sub/cover.png"));
    }

    [Fact]
    public void NormalizeStoredPathValidPathReturnsCanonicalLeadingSlashForm()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal("/uploads/cover.png", paths.NormalizeStoredPath("/uploads/cover.png"));
    }

    [Fact]
    public void NormalizeStoredPathBackslashesAndUriEncodedNormalizesAndDecodes()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal("/uploads/my image.png", paths.NormalizeStoredPath(@"uploads\my%20image.png"));
    }

    [Fact]
    public void ResolveUploadAbsolutePathValidPathReturnsCombinedFilesystemPath()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var resolved = paths.ResolveUploadAbsolutePath("/uploads/cover.png");

        Assert.Equal(Path.Combine(paths.UploadRootPath, "cover.png"), resolved);
    }

    [Fact]
    public void ResolveUploadAbsolutePathInvalidPathThrows()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Throws<InvalidOperationException>(() => paths.ResolveUploadAbsolutePath("../etc/passwd"));
    }

    [Fact]
    public void EnumerateCacheVariantPathsPathInvalidReturnsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Empty(paths.EnumerateCacheVariantPaths("not-an-uploads-path"));
    }

    [Fact]
    public void EnumerateCacheVariantPathsCacheRootMissingReturnsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Empty(paths.EnumerateCacheVariantPaths("/uploads/cover.png"));
    }

    [Fact]
    public void EnumerateCacheVariantPathsNoVariantDirectoriesReturnsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);
        Directory.CreateDirectory(paths.CacheRootPath);

        Assert.Empty(paths.EnumerateCacheVariantPaths("/uploads/cover.png"));
    }

    [Fact]
    public void EnumerateCacheVariantPathsFindsVariantsAcrossFolders()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);
        Directory.CreateDirectory(Path.Combine(paths.CacheRootPath, "100x200"));
        Directory.CreateDirectory(Path.Combine(paths.CacheRootPath, "autoxauto"));
        var variant1 = Path.Combine(paths.CacheRootPath, "100x200", "cover.webp");
        var variant2 = Path.Combine(paths.CacheRootPath, "autoxauto", "cover.jpg");
        File.WriteAllBytes(variant1, Array.Empty<byte>());
        File.WriteAllBytes(variant2, Array.Empty<byte>());

        var results = paths.EnumerateCacheVariantPaths("/uploads/cover.png").ToList();

        Assert.Contains(variant1, results);
        Assert.Contains(variant2, results);
    }

    [Fact]
    public void EnumerateCacheVariantPathsDoesNotIncludeUnrelatedFiles()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);
        Directory.CreateDirectory(Path.Combine(paths.CacheRootPath, "100x200"));
        var unrelated = Path.Combine(paths.CacheRootPath, "100x200", "other.webp");
        File.WriteAllBytes(unrelated, Array.Empty<byte>());

        var results = paths.EnumerateCacheVariantPaths("/uploads/cover.png").ToList();

        Assert.DoesNotContain(unrelated, results);
    }

    [Fact]
    public void NormalizeUploadsPathBlankInputReturnsDefault()
    {
        Assert.Equal("uploads", UploadStoragePaths.NormalizeUploadsPath(null));
    }

    [Fact]
    public void NormalizeUploadsPathOnlySlashesReturnsDefault()
    {
        Assert.Equal("uploads", UploadStoragePaths.NormalizeUploadsPath("///"));
    }

    [Fact]
    public void NormalizeUploadsPathStripsSlashesAndConvertsBackslashes()
    {
        Assert.Equal("media/files", UploadStoragePaths.NormalizeUploadsPath(@"\media\files\"));
    }
}
