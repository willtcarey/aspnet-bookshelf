using Bookshelf.Models;

namespace Bookshelf.Tests.Models;

public class ImageUploadStaticTests
{
    [Fact]
    public void IsValidDimensionNullReturnsTrue()
    {
        Assert.True(ImageUpload.IsValidDimension(null));
    }

    [Fact]
    public void IsValidDimensionInRangeReturnsTrue()
    {
        Assert.True(ImageUpload.IsValidDimension(2000));
    }

    [Fact]
    public void IsValidDimensionOutOfRangeReturnsFalse()
    {
        Assert.False(ImageUpload.IsValidDimension(0));
    }

    [Fact]
    public void ResolveFormatNullOrWhitespaceReturnsWebpDefault()
    {
        Assert.Equal("webp", ImageUpload.ResolveFormat(null)!.Name);
    }

    [Fact]
    public void ResolveFormatKnownFormatReturnsMatchedFormat()
    {
        Assert.Equal("jpg", ImageUpload.ResolveFormat("jpg")!.Name);
    }

    [Fact]
    public void ResolveFormatUnknownFormatReturnsNull()
    {
        Assert.Null(ImageUpload.ResolveFormat("tiff"));
    }

    [Fact]
    public void GetContentTypeFromPathKnownExtensionReturnsImageMimeType()
    {
        Assert.Equal("image/png", ImageUpload.GetContentTypeFromPath("/uploads/cover.png"));
    }

    [Fact]
    public void GetContentTypeFromPathUnknownExtensionReturnsOctetStream()
    {
        Assert.Equal("application/octet-stream", ImageUpload.GetContentTypeFromPath("/uploads/cover.unknown"));
    }
}
