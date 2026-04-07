namespace Bookshelf.Services;

public interface IImageProcessor
{
    Task<Stream> ResizeAsync(Stream source, int width, int height, string format = "webp");
}
