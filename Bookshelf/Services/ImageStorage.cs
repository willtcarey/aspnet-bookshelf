using System.Globalization;

namespace Bookshelf.Services;

public sealed record ImageUploadKey(Guid Id, string Extension)
{
    public string FileName => $"{Id:N}{Extension}";

    public static ImageUploadKey? Parse(string? key)
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
            ? new ImageUploadKey(id, extension)
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

public sealed class ImageStorage
{
    private readonly UploadStoragePaths _paths;

    public ImageStorage(UploadStoragePaths paths)
    {
        _paths = paths;
    }

    public ImageUploadKey? GetKeyFromStoredPath(string? path)
    {
        var normalizedPath = _paths.NormalizeStoredPath(path);
        return normalizedPath is null
            ? null
            : ImageUploadKey.Parse(Path.GetFileName(normalizedPath));
    }

    public string? BuildUrl(string? path, int? width = null, int? height = null, string? format = null)
    {
        var key = GetKeyFromStoredPath(path);
        if (key is null)
        {
            return null;
        }

        var imagePath = $"/images/{Uri.EscapeDataString(key.FileName)}";
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

    public string BuildStoredPath(ImageUploadKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return $"{_paths.UploadRequestPath}/{key.FileName}";
    }
}
