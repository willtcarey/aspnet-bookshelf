using Bookshelf.Services;
using Bookshelf.Tests.TestSupport;

namespace Bookshelf.Tests.Services;

public class ImageStorageTests
{
    private const string ValidGuid = "11111111111111111111111111111111";
    private const string ValidStoredPath = $"/uploads/{ValidGuid}.png";

    private static ImageStorage Build()
    {
        var paths = TestUploadPaths.Create(Path.GetTempPath());
        return new ImageStorage(paths);
    }

    [Fact]
    public void BuildUrlInvalidPathReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildUrl("not/in/uploads"));
    }

    [Fact]
    public void BuildUrlNullPathReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildUrl(null!));
    }

    [Fact]
    public void BuildUrlNoQueryParamsReturnsBasePath()
    {
        var storage = Build();

        Assert.Equal($"/images/{ValidGuid}.png", storage.BuildUrl(ValidStoredPath));
    }

    [Fact]
    public void BuildUrlWithWidthAddsWidthParam()
    {
        var storage = Build();

        Assert.Equal($"/images/{ValidGuid}.png?w=100", storage.BuildUrl(ValidStoredPath, width: 100));
    }

    [Fact]
    public void BuildUrlWithHeightAddsHeightParam()
    {
        var storage = Build();

        Assert.Equal($"/images/{ValidGuid}.png?h=200", storage.BuildUrl(ValidStoredPath, height: 200));
    }

    [Fact]
    public void BuildUrlWithFormatAddsFormatParam()
    {
        var storage = Build();

        Assert.Equal($"/images/{ValidGuid}.png?format=webp", storage.BuildUrl(ValidStoredPath, format: "webp"));
    }

    [Fact]
    public void BuildUrlWithAllParamsCombinesQueryString()
    {
        var storage = Build();

        Assert.Equal(
            $"/images/{ValidGuid}.png?w=100&h=200&format=webp",
            storage.BuildUrl(ValidStoredPath, 100, 200, "webp"));
    }

    [Fact]
    public void BuildUrlZeroWidthOmitsParam()
    {
        var storage = Build();

        Assert.Equal($"/images/{ValidGuid}.png", storage.BuildUrl(ValidStoredPath, width: 0));
    }

    [Fact]
    public void BuildUrlZeroHeightOmitsParam()
    {
        var storage = Build();

        Assert.Equal($"/images/{ValidGuid}.png", storage.BuildUrl(ValidStoredPath, height: 0));
    }

    [Fact]
    public void BuildUrlWhitespaceFormatOmitsParam()
    {
        var storage = Build();

        Assert.Equal($"/images/{ValidGuid}.png", storage.BuildUrl(ValidStoredPath, format: "  "));
    }

    [Fact]
    public void BuildStoredPathValidGuidKeyReturnsPath()
    {
        var storage = Build();

        Assert.Equal($"/uploads/{ValidGuid}.png", storage.BuildStoredPath($"{ValidGuid}.png"));
    }

    [Fact]
    public void BuildStoredPathNullKeyReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath(null));
    }

    [Fact]
    public void BuildStoredPathWhitespaceKeyReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath("   "));
    }

    [Fact]
    public void BuildStoredPathKeyWithSlashReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath($"sub/{ValidGuid}.png"));
    }

    [Fact]
    public void BuildStoredPathKeyWithBackslashReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath($@"sub\{ValidGuid}.png"));
    }

    [Fact]
    public void BuildStoredPathKeyWithNullByteReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath($"{ValidGuid}\0.png"));
    }

    [Fact]
    public void BuildStoredPathMissingExtensionReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath(ValidGuid));
    }

    [Fact]
    public void BuildStoredPathDotOnlyExtensionReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath($"{ValidGuid}."));
    }

    [Fact]
    public void BuildStoredPathOversizeExtensionReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath($"{ValidGuid}.toolongextension"));
    }

    [Fact]
    public void BuildStoredPathNonAlphaNumExtensionReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath($"{ValidGuid}.p_g"));
    }

    [Fact]
    public void BuildStoredPathNonGuidStemReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildStoredPath("not-a-guid.png"));
    }

    [Fact]
    public void BuildUrlStoredPathWithNonGuidFilenameReturnsNull()
    {
        var storage = Build();

        Assert.Null(storage.BuildUrl("/uploads/cover.png"));
    }
}
