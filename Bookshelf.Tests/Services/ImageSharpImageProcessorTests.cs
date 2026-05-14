using Bookshelf.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bookshelf.Tests.Services;

public class ImageSharpImageProcessorTests
{
    private readonly ImageSharpImageProcessor _processor = new();

    [Fact]
    public async Task ResizeAsync_NullSource_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _processor.ResizeAsync(null!, 100, 100));
    }

    [Fact]
    public async Task ResizeAsync_NonPositiveWidth_Throws()
    {
        await using var source = await BuildPngStream(50, 50);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _processor.ResizeAsync(source, 0, 100));
    }

    [Fact]
    public async Task ResizeAsync_NonPositiveHeight_Throws()
    {
        await using var source = await BuildPngStream(50, 50);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _processor.ResizeAsync(source, 100, 0));
    }

    [Fact]
    public async Task ResizeAsync_ValidImage_ReturnsResizedStream()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, "png");

        var info = await Image.IdentifyAsync(resized);
        Assert.True(info.Width <= 100);
        Assert.True(info.Height <= 100);
    }

    [Fact]
    public async Task ResizeAsync_ImageSmallerThanTarget_DoesNotResize()
    {
        await using var source = await BuildPngStream(50, 50);

        await using var resized = await _processor.ResizeAsync(source, 200, 200, "png");

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal(50, info.Width);
        Assert.Equal(50, info.Height);
    }

    [Fact]
    public async Task ResizeAsync_JpgFormat_EncodesAsJpeg()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, "jpg");

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal("JPEG", info.Metadata.DecodedImageFormat?.Name);
    }

    [Fact]
    public async Task ResizeAsync_DefaultFormat_EncodesAsWebp()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100);

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal("Webp", info.Metadata.DecodedImageFormat?.Name);
    }

    [Fact]
    public void NormalizeFormat_KnownAlias_ReturnsCanonical()
    {
        Assert.Equal("jpg", ImageSharpImageProcessor.NormalizeFormat("JPEG"));
    }

    [Fact]
    public void NormalizeFormat_UnknownFormat_ReturnsWebpDefault()
    {
        Assert.Equal("webp", ImageSharpImageProcessor.NormalizeFormat("tiff"));
    }

    [Fact]
    public void NormalizeFormat_NullFormat_ReturnsWebpDefault()
    {
        Assert.Equal("webp", ImageSharpImageProcessor.NormalizeFormat(null));
    }

    [Fact]
    public void NormalizeFormat_JpgFormat_ReturnsJpg()
    {
        Assert.Equal("jpg", ImageSharpImageProcessor.NormalizeFormat("jpg"));
    }

    private static async Task<MemoryStream> BuildPngStream(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }
}
