namespace Ombor.Application.Models;

public record struct FileUploadResult(
    string OriginalFileName,
    string Url,
    string? ThumbnailUrl);