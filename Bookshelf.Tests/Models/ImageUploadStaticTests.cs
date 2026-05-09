using Bookshelf.Models;

namespace Bookshelf.Tests.Models;

public class ImageUploadStaticTests
{
    [Fact]
    public void IsValidDimension_Null_ReturnsTrue()
    {
        Assert.True(ImageUpload.IsValidDimension(null));
    }

    [Fact]
    public void IsValidDimension_InRange_ReturnsTrue()
    {
        Assert.True(ImageUpload.IsValidDimension(2000));
    }

    [Fact]
    public void IsValidDimension_OutOfRange_ReturnsFalse()
    {
        Assert.False(ImageUpload.IsValidDimension(0));
    }

    [Fact]
    public void ResolveFormat_NullOrWhitespace_ReturnsWebpDefault()
    {
        Assert.Equal("webp", ImageUpload.ResolveFormat(null)!.Name);
    }

    [Fact]
    public void ResolveFormat_KnownFormat_ReturnsMatchedFormat()
    {
        Assert.Equal("jpg", ImageUpload.ResolveFormat("jpg")!.Name);
    }

    [Fact]
    public void ResolveFormat_UnknownFormat_ReturnsNull()
    {
        Assert.Null(ImageUpload.ResolveFormat("tiff"));
    }

    [Fact]
    public void GetContentTypeFromPath_KnownExtension_ReturnsImageMimeType()
    {
        Assert.Equal("image/png", ImageUpload.GetContentTypeFromPath("/uploads/cover.png"));
    }

    [Fact]
    public void GetContentTypeFromPath_UnknownExtension_ReturnsOctetStream()
    {
        Assert.Equal("application/octet-stream", ImageUpload.GetContentTypeFromPath("/uploads/cover.unknown"));
    }
}
