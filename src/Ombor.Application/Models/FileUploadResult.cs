namespace Ombor.Application.Models;

/// <summary>A stored upload.</summary>
/// <param name="FileName">The stored (random, GUID-based) file name.</param>
/// <param name="OriginalFileName">The name the client sent, kept for display.</param>
/// <param name="Url">Public URL of the original, relative to the API base.</param>
/// <param name="ThumbnailUrl">Public URL of the thumbnail, or null for non-images or a failed thumbnail.</param>
/// <param name="ContentType">The MIME type proven by the file's content (never the client's claim).</param>
public record struct FileUploadResult(
    string FileName,
    string OriginalFileName,
    string Url,
    string? ThumbnailUrl,
    string ContentType = "application/octet-stream");
