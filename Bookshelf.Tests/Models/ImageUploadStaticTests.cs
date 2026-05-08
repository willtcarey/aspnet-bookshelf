using Bookshelf.Models;

namespace Bookshelf.Tests.Models;

public class ImageUploadStaticTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(4000, true)]
    [InlineData(2000, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(4001, false)]
    [InlineData(int.MaxValue, false)]
    public void IsValidDimension_BoundsCheck(int? value, bool expected)
    {
        Assert.Equal(expected, ImageUpload.IsValidDimension(value));
    }

    [Theory]
    [InlineData(null, "webp")]
    [InlineData("", "webp")]
    [InlineData("   ", "webp")]
    [InlineData("webp", "webp")]
    [InlineData("WEBP", "webp")]
    [InlineData("jpg", "jpg")]
    [InlineData("jpeg", "jpg")]
    [InlineData("JPG", "jpg")]
    [InlineData("png", "png")]
    [InlineData("PNG", "png")]
    [InlineData("  jpg  ", "jpg")]
    public void ResolveFormat_KnownFormats_ResolveToExpectedName(string? input, string expectedName)
    {
        var format = ImageUpload.ResolveFormat(input);

        Assert.NotNull(format);
        Assert.Equal(expectedName, format!.Name);
    }

    [Theory]
    [InlineData("tiff")]
    [InlineData("bmp")]
    [InlineData("svg")]
    [InlineData("not-a-format")]
    public void ResolveFormat_UnknownFormats_ReturnNull(string input)
    {
        Assert.Null(ImageUpload.ResolveFormat(input));
    }

    [Theory]
    [InlineData("/uploads/cover.jpg", "image/jpeg")]
    [InlineData("/uploads/cover.jpeg", "image/jpeg")]
    [InlineData("/uploads/cover.png", "image/png")]
    [InlineData("/uploads/cover.gif", "image/gif")]
    public void GetContentTypeFromPath_KnownExtensions_ReturnImageMimeType(string path, string expected)
    {
        Assert.Equal(expected, ImageUpload.GetContentTypeFromPath(path));
    }

    [Theory]
    [InlineData("/uploads/cover.unknown")]
    [InlineData("/uploads/no-extension")]
    [InlineData("")]
    public void GetContentTypeFromPath_UnknownOrMissingExtension_ReturnsOctetStream(string path)
    {
        Assert.Equal("application/octet-stream", ImageUpload.GetContentTypeFromPath(path));
    }
}
