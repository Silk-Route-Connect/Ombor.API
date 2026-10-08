namespace Ombor.Application.Helpers;

/// <summary>
/// Identifies an upload by its leading bytes (magic numbers), so neither the file name nor the client's
/// <c>Content-Type</c> decides what is stored and later served. Only the types uploads accept are known.
/// </summary>
internal static class FileSignatures
{
    /// <summary>How many leading bytes <see cref="Detect"/> needs to recognise every known type.</summary>
    public const int HeaderLength = 12;

    public const string Png = "image/png";
    public const string Jpeg = "image/jpeg";
    public const string Gif = "image/gif";
    public const string Webp = "image/webp";
    public const string Pdf = "application/pdf";

    private static readonly Dictionary<string, string> ContentTypeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = Png,
        [".jpg"] = Jpeg,
        [".jpeg"] = Jpeg,
        [".gif"] = Gif,
        [".webp"] = Webp,
        [".pdf"] = Pdf,
    };

    /// <summary>The content type the leading bytes prove, or null when they match no known type.</summary>
    public static string? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return Png;
        }

        if (header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
        {
            return Jpeg;
        }

        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
        {
            return Gif;
        }

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            return Webp;
        }

        return header.StartsWith("%PDF-"u8) ? Pdf : null;
    }

    /// <summary>The content type a file with <paramref name="extension"/> must contain, or null for an unknown extension.</summary>
    public static string? ExpectedFor(string extension) =>
        ContentTypeByExtension.TryGetValue(extension, out var contentType) ? contentType : null;

    public static bool IsImage(string contentType) => contentType.StartsWith("image/", StringComparison.Ordinal);
}
