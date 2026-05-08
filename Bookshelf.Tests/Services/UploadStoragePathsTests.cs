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
    public void Constructor_FallsBackToContentRootWwwrootWhenWebRootIsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: "", contentRoot: _root);

        Assert.Equal(Path.Combine(_root, "wwwroot", "uploads"), paths.UploadRootPath);
    }

    [Fact]
    public void Constructor_CreatesUploadRootDirectory()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.True(Directory.Exists(paths.UploadRootPath));
    }

    [Fact]
    public void Constructor_BuildsRequestPathFromUploadsConfig()
    {
        var paths = TestUploadPaths.Create(webRoot: _root, uploadsConfig: "media/files");

        Assert.Equal("/media/files", paths.UploadRequestPath);
        Assert.Equal("media/files", paths.UploadsPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_FallsBackToDefaultUploadsWhenConfigBlank(string? configured)
    {
        var paths = TestUploadPaths.Create(webRoot: _root, uploadsConfig: configured);

        Assert.Equal("uploads", paths.UploadsPath);
    }

    [Theory]
    [InlineData("/uploads/")]
    [InlineData("uploads/")]
    [InlineData("\\uploads\\")]
    public void Constructor_StripsSurroundingSlashesFromConfiguredUploadsPath(string configured)
    {
        var paths = TestUploadPaths.Create(webRoot: _root, uploadsConfig: configured);

        Assert.Equal("uploads", paths.UploadsPath);
    }

    [Fact]
    public void Constructor_ConvertsBackslashesInUploadsConfigToForwardSlashes()
    {
        var paths = TestUploadPaths.Create(webRoot: _root, uploadsConfig: "media\\files");

        Assert.Equal("media/files", paths.UploadsPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeStoredPath_BlankInput_ReturnsNull(string? input)
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath(input));
    }

    [Fact]
    public void NormalizeStoredPath_PathContainingNullByte_ReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("uploads/foo\0.png"));
    }

    [Fact]
    public void NormalizeStoredPath_PathNotInUploadsRoot_ReturnsNull()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath("other/cover.png"));
    }

    [Theory]
    [InlineData("uploads/.")]
    [InlineData("uploads/..")]
    public void NormalizeStoredPath_DotFileNames_ReturnNull(string input)
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Null(paths.NormalizeStoredPath(input));
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
    public void NormalizeStoredPath_PathWithBackslashes_NormalizesToForwardSlashes()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal("/uploads/cover.png", paths.NormalizeStoredPath(@"uploads\cover.png"));
    }

    [Fact]
    public void NormalizeStoredPath_UriEncodedInput_DecodesBeforeNormalizing()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal("/uploads/my image.png", paths.NormalizeStoredPath("uploads/my%20image.png"));
    }

    [Fact]
    public void NormalizeStoredPath_TrimsSurroundingWhitespace()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Equal("/uploads/cover.png", paths.NormalizeStoredPath("  /uploads/cover.png  "));
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
    public void BuildCachePath_WithBothDimensions_BuildsWidthByHeightFolder()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/cover.png", 100, 200, ".webp");

        Assert.Equal(Path.Combine(paths.CacheRootPath, "100x200", "cover.webp"), cachePath);
    }

    [Fact]
    public void BuildCachePath_WithWidthOnly_UsesAutoForHeight()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/cover.png", 100, null, ".webp");

        Assert.Equal(Path.Combine(paths.CacheRootPath, "100xauto", "cover.webp"), cachePath);
    }

    [Fact]
    public void BuildCachePath_WithHeightOnly_UsesAutoForWidth()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/cover.png", null, 200, ".webp");

        Assert.Equal(Path.Combine(paths.CacheRootPath, "autox200", "cover.webp"), cachePath);
    }

    [Fact]
    public void BuildCachePath_WithBothNull_UsesAutoForBoth()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        var cachePath = paths.BuildCachePath("/uploads/cover.png", null, null, ".webp");

        Assert.Equal(Path.Combine(paths.CacheRootPath, "autoxauto", "cover.webp"), cachePath);
    }

    [Fact]
    public void EnumerateCacheVariantPaths_WhenPathInvalid_ReturnsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Empty(paths.EnumerateCacheVariantPaths("not-an-uploads-path"));
    }

    [Fact]
    public void EnumerateCacheVariantPaths_WhenCacheRootMissing_ReturnsEmpty()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);

        Assert.Empty(paths.EnumerateCacheVariantPaths("/uploads/cover.png"));
    }

    [Fact]
    public void EnumerateCacheVariantPaths_ReturnsVariantsAcrossFolders()
    {
        var paths = TestUploadPaths.Create(webRoot: _root);
        Directory.CreateDirectory(Path.Combine(paths.CacheRootPath, "100x200"));
        Directory.CreateDirectory(Path.Combine(paths.CacheRootPath, "autoxauto"));
        var variant1 = Path.Combine(paths.CacheRootPath, "100x200", "cover.webp");
        var variant2 = Path.Combine(paths.CacheRootPath, "autoxauto", "cover.jpg");
        var unrelated = Path.Combine(paths.CacheRootPath, "100x200", "other.webp");
        File.WriteAllBytes(variant1, Array.Empty<byte>());
        File.WriteAllBytes(variant2, Array.Empty<byte>());
        File.WriteAllBytes(unrelated, Array.Empty<byte>());

        var results = paths.EnumerateCacheVariantPaths("/uploads/cover.png").ToList();

        Assert.Contains(variant1, results);
        Assert.Contains(variant2, results);
        Assert.DoesNotContain(unrelated, results);
    }
}
