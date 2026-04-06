using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Bookshelf.Services;

public class ImageSharpImageProcessor : IImageProcessor
{
    public async Task<Stream> ResizeAsync(Stream source, int width, int height, string format = "webp")
    {
        ArgumentNullException.ThrowIfNull(source);

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (source.CanSeek)
        {
            source.Position = 0;
        }

        using var image = await Image.LoadAsync(source);

        if (image.Width > width || image.Height > height)
        {
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(width, height)
            }));
        }

        var output = new MemoryStream();
        await image.SaveAsync(output, ResolveEncoder(format));
        output.Position = 0;

        return output;
    }

    private static IImageEncoder ResolveEncoder(string format)
    {
        return NormalizeFormat(format) switch
        {
            "jpg" => new JpegEncoder(),
            "png" => new PngEncoder(),
            _ => new WebpEncoder()
        };
    }

    private static string NormalizeFormat(string? format)
    {
        return format?.Trim().ToLowerInvariant() switch
        {
            "jpeg" => "jpg",
            "jpg" => "jpg",
            "png" => "png",
            _ => "webp"
        };
    }
}
