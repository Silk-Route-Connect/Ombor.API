using Ombor.Application.Models;

namespace Ombor.Application.Helpers;

public static class ImageHelper
{
    public static ThumbnailFormat GetThumbnailFormat(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        return extension.Trim().ToLowerInvariant() switch
        {
            ".png" => ThumbnailFormat.Png,
            ".jpg" or ".jpeg" => ThumbnailFormat.Jpg,
            ".webp" => ThumbnailFormat.Webp,
            ".gif" => ThumbnailFormat.Gif,
            _ => throw new NotSupportedException($"Unsupported image format: {extension}")
        };
    }
}
