namespace Bookshelf.Services;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType);
    Task<Stream?> GetAsync(string path);
    Task DeleteAsync(string path);
    string GetUrl(string path);
}
