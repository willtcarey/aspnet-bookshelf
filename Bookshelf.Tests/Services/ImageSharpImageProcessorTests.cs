using Bookshelf.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ResizeAsync_NonPositiveWidth_Throws(int width)
    {
        await using var source = await BuildPngStream(50, 50);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _processor.ResizeAsync(source, width, 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ResizeAsync_NonPositiveHeight_Throws(int height)
    {
        await using var source = await BuildPngStream(50, 50);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _processor.ResizeAsync(source, 100, height));
    }

    [Fact]
    public async Task ResizeAsync_ImageLargerThanTarget_ScalesDownPreservingAspect()
    {
        await using var source = await BuildPngStream(400, 200);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, "png");

        var info = await Image.IdentifyAsync(resized);
        Assert.True(info.Width <= 100);
        Assert.True(info.Height <= 100);
    }

    [Fact]
    public async Task ResizeAsync_ImageSmallerThanTarget_LeavesDimensionsUnchanged()
    {
        await using var source = await BuildPngStream(40, 60);

        await using var resized = await _processor.ResizeAsync(source, 1000, 1000, "png");

        var info = await Image.IdentifyAsync(resized);
        Assert.Equal(40, info.Width);
        Assert.Equal(60, info.Height);
    }

    [Fact]
    public async Task ResizeAsync_OutputStreamPositionedAtStart()
    {
        await using var source = await BuildPngStream(50, 50);

        await using var resized = await _processor.ResizeAsync(source, 100, 100);

        Assert.Equal(0, resized.Position);
    }

    [Fact]
    public async Task ResizeAsync_RewindsSeekableSourceBeforeReading()
    {
        var source = await BuildPngStream(50, 50);
        source.Position = source.Length;

        await using var resized = await _processor.ResizeAsync(source, 100, 100);

        Assert.NotEqual(0, resized.Length);
    }

    [Theory]
    [InlineData("jpg", typeof(JpegFormat))]
    [InlineData("jpeg", typeof(JpegFormat))]
    [InlineData("JPG", typeof(JpegFormat))]
    [InlineData("png", typeof(PngFormat))]
    [InlineData("  PNG  ", typeof(PngFormat))]
    [InlineData("webp", typeof(WebpFormat))]
    [InlineData("unknown", typeof(WebpFormat))]
    [InlineData("", typeof(WebpFormat))]
    [InlineData(null, typeof(WebpFormat))]
    public async Task ResizeAsync_FormatString_DeterminesOutputEncoder(string? format, Type expectedFormatType)
    {
        await using var source = await BuildPngStream(50, 50);

        await using var resized = await _processor.ResizeAsync(source, 100, 100, format!);

        var detected = await Image.DetectFormatAsync(resized);
        Assert.IsAssignableFrom(expectedFormatType, detected);
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
