using Ombor.Application.Models;

namespace Ombor.Application.Helpers;

public static class ImageHelpers
{
    public static ThumbnailFormat GetFormat(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        return extension.Trim().ToLowerInvariant() switch
        {
            ".png" => ThumbnailFormat.Png,
            ".jpg" => ThumbnailFormat.Jpg,
            ".jpeg" => ThumbnailFormat.Jpeg,
            ".webp" => ThumbnailFormat.Webp,
            ".gif" => ThumbnailFormat.Gif,
            _ => throw new NotSupportedException($"Unsupported image format: {extension}")
        };
    }
}
