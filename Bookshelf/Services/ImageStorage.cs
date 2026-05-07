using System.Globalization;

namespace Bookshelf.Services;

public sealed class ImageStorage
{
    private readonly UploadStoragePaths _paths;

    public ImageStorage(UploadStoragePaths paths)
    {
        _paths = paths;
    }

    public string? BuildUrl(string? path, int? width = null, int? height = null, string? format = null)
    {
        var fileName = GetFileNameFromStoredPath(path);
        if (fileName is null)
        {
            return null;
        }

        var imagePath = $"/images/{Uri.EscapeDataString(fileName)}";
        var queryParameters = new List<string>();

        if (width is > 0)
        {
            queryParameters.Add($"w={width.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (height is > 0)
        {
            queryParameters.Add($"h={height.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (!string.IsNullOrWhiteSpace(format))
        {
            queryParameters.Add($"format={Uri.EscapeDataString(format.Trim())}");
        }

        return queryParameters.Count == 0
            ? imagePath
            : $"{imagePath}?{string.Join('&', queryParameters)}";
    }

    public string? BuildStoredPath(string? key)
    {
        var fileName = NormalizeKey(key);
        return fileName is null
            ? null
            : $"{_paths.UploadRequestPath}/{fileName}";
    }

    private string? GetFileNameFromStoredPath(string? path)
    {
        var normalizedPath = _paths.NormalizeStoredPath(path);
        return normalizedPath is null
            ? null
            : NormalizeKey(Path.GetFileName(normalizedPath));
    }

    private static string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalizedKey = Uri.UnescapeDataString(key).Trim();
        if (normalizedKey.Contains('\0', StringComparison.Ordinal)
            || normalizedKey.Contains('/', StringComparison.Ordinal)
            || normalizedKey.Contains('\\', StringComparison.Ordinal))
        {
            return null;
        }

        var extension = Path.GetExtension(normalizedKey);
        if (!IsSafeExtension(extension))
        {
            return null;
        }

        var fileStem = Path.GetFileNameWithoutExtension(normalizedKey);
        return Guid.TryParseExact(fileStem, "N", out var id)
            ? $"{id:N}{extension}"
            : null;
    }

    private static bool IsSafeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension) || extension.Length is < 2 or > 10)
        {
            return false;
        }

        foreach (var character in extension[1..])
        {
            if (!char.IsAsciiLetterOrDigit(character))
            {
                return false;
            }
        }

        return true;
    }
}
