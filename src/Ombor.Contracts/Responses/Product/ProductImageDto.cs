namespace Ombor.Contracts.Responses.Product;

public sealed record ProductImageDto(
    int Id,
    string Name,
    string OriginalUrl,
    string? ThumbnailUrl);
