using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;

namespace Bookshelf.Tests.Services;

public class UploadStoragePathsTests : IDisposable
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
    public void Constructor_UsesWebRootPathWhenSet()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal(Path.Combine(_root, "uploads"), paths.UploadRootPath);
    }

    [Fact]
    public void Constructor_FallsBackToContentRootWwwrootWhenWebRootEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: "", contentRoot: _root);

        Assert.Equal(Path.Combine(_root, "wwwroot", "uploads"), paths.UploadRootPath);
    }

    [Fact]
    public void NormalizeStoredPath_BlankInput_ReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath(null));
    }

    [Fact]
    public void NormalizeStoredPath_NullByteInPath_ReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("uploads/foo\0.png"));
    }

    [Fact]
    public void NormalizeStoredPath_NotInUploadsRoot_ReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("other/cover.png"));
    }

    [Fact]
    public void NormalizeStoredPath_DotFileName_ReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("uploads/.."));
    }

    [Fact]
    public void NormalizeStoredPath_NestedPath_ReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("uploads/sub/cover.png"));
    }

    [Fact]
    public void NormalizeStoredPath_ValidPath_ReturnsCanonicalLeadingSlashForm()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal("/uploads/cover.png", paths.NormalizeStoredPath("/uploads/cover.png"));
    }

    [Fact]
    public void NormalizeStoredPath_BackslashesAndUriEncoded_NormalizesAndDecodes()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal("/uploads/my image.png", paths.NormalizeStoredPath(@"uploads\my%20image.png"));
    }

    [Fact]
    public void ResolveUploadAbsolutePath_ValidPath_ReturnsCombinedFilesystemPath()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var resolved = paths.ResolveUploadAbsolutePath("/uploads/cover.png");

        Assert.Equal(Path.Combine(paths.UploadRootPath, "cover.png"), resolved);
    }

    [Fact]
    public void ResolveUploadAbsolutePath_InvalidPath_Throws()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Throws<InvalidOperationException>(() => paths.ResolveUploadAbsolutePath("../etc/passwd"));
    }

    [Fact]
    public void BuildCachePath_BothDimensions_BuildsWidthByHeightFolder()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/cover.png", 100, 200, ".webp");

        Assert.Equal(Path.Combine(paths.CacheRootPath, "100x200", "cover.webp"), cachePath);
    }

    [Fact]
    public void BuildCachePath_WidthOnly_UsesAutoForHeight()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/cover.png", 100, null, ".webp");

        Assert.Equal(Path.Combine(paths.CacheRootPath, "100xauto", "cover.webp"), cachePath);
    }

    [Fact]
    public void BuildCachePath_HeightOnly_UsesAutoForWidth()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/cover.png", null, 200, ".webp");

        Assert.Equal(Path.Combine(paths.CacheRootPath, "autox200", "cover.webp"), cachePath);
    }

    [Fact]
    public void BuildCachePath_BothNull_UsesAutoForBoth()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/cover.png", null, null, ".webp");

        Assert.Equal(Path.Combine(paths.CacheRootPath, "autoxauto", "cover.webp"), cachePath);
    }

    [Fact]
    public void BuildCachePath_PreservesFilenameAndAppliesExtension()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/photo.jpg", 100, 200, ".webp");

        Assert.EndsWith("photo.webp", cachePath);
    }

    [Fact]
    public void EnumerateCacheVariantPaths_PathInvalid_ReturnsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Empty(paths.EnumerateCacheVariantPaths("not-an-uploads-path"));
    }

    [Fact]
    public void EnumerateCacheVariantPaths_CacheRootMissing_ReturnsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Empty(paths.EnumerateCacheVariantPaths("/uploads/cover.png"));
    }

    [Fact]
    public void EnumerateCacheVariantPaths_NoVariantDirectories_ReturnsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);
        Directory.CreateDirectory(paths.CacheRootPath);

        Assert.Empty(paths.EnumerateCacheVariantPaths("/uploads/cover.png"));
    }

    [Fact]
    public void EnumerateCacheVariantPaths_FindsVariantsAcrossFolders()
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
    public void EnumerateCacheVariantPaths_DoesNotIncludeUnrelatedFiles()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);
        Directory.CreateDirectory(Path.Combine(paths.CacheRootPath, "100x200"));
        var unrelated = Path.Combine(paths.CacheRootPath, "100x200", "other.webp");
        File.WriteAllBytes(unrelated, Array.Empty<byte>());

        var results = paths.EnumerateCacheVariantPaths("/uploads/cover.png").ToList();

        Assert.DoesNotContain(unrelated, results);
    }

    [Fact]
    public void NormalizeUploadsPath_BlankInput_ReturnsDefault()
    {
        Assert.Equal("uploads", UploadStoragePaths.NormalizeUploadsPath(null));
    }

    [Fact]
    public void NormalizeUploadsPath_OnlySlashes_ReturnsDefault()
    {
        Assert.Equal("uploads", UploadStoragePaths.NormalizeUploadsPath("///"));
    }

    [Fact]
    public void NormalizeUploadsPath_StripsSlashesAndConvertsBackslashes()
    {
        Assert.Equal("media/files", UploadStoragePaths.NormalizeUploadsPath(@"\media\files\"));
    }
}
