namespace Ombor.Application.Models;

public record struct FileUploadResult(
    string FileName,
    string OriginalFileName,
    string Url,
    string? ThumbnailUrl);