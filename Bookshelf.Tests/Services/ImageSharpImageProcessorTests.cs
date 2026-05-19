using Bookshelf.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bookshelf.Tests.Services;

public class ImageSharpImageProcessorTests
{
    private readonly ImageSharpImageProcessor _processor = new();

    [Fact]
    public async Task ResizeAsyncNullSourceThrows()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _processor.ResizeAsync(null!, 100, 100));
    }

    [Fact]
    public async Task ResizeAsyncNonPositiveWidthThrows()
    {
        await using var source = await BuildPngStream(50, 50);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _processor.ResizeAsync(source, 0, 100));
    }

    [Fact]
    public async Task ResizeAsyncNonPositiveHeightThrows()
    {
        await using var source = await BuildPngStream(50, 50);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _processor.ResizeAsync(source, 100, 0));
    }

    [Fact]
    public async Task ResizeAsyncValidImageReturnsResizedStream()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, "png");

        var info = await Image.IdentifyAsync(resized);
        Assert.True(info.Width <= 100);
        Assert.True(info.Height <= 100);
    }

    [Fact]
    public async Task ResizeAsyncImageSmallerThanTargetDoesNotResize()
    {
        await using var source = await BuildPngStream(50, 50);

        await using var resized = await _processor.ResizeAsync(source, 200, 200, "png");

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal(50, info.Width);
        Assert.Equal(50, info.Height);
    }

    [Fact]
    public async Task ResizeAsyncJpgFormatEncodesAsJpeg()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, "jpg");

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal("JPEG", info.Metadata.DecodedImageFormat?.Name);
    }

    [Fact]
    public async Task ResizeAsyncJpegAliasEncodesAsJpeg()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, "JPEG");

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal("JPEG", info.Metadata.DecodedImageFormat?.Name);
    }

    [Fact]
    public async Task ResizeAsyncDefaultFormatEncodesAsWebp()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100);

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal("Webp", info.Metadata.DecodedImageFormat?.Name);
    }

    [Fact]
    public async Task ResizeAsyncUnknownFormatEncodesAsWebp()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, "tiff");

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal("Webp", info.Metadata.DecodedImageFormat?.Name);
    }

    [Fact]
    public async Task ResizeAsyncNullFormatEncodesAsWebp()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, null!);

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal("Webp", info.Metadata.DecodedImageFormat?.Name);
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
