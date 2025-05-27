namespace Ombor.Contracts.Responses.Product;

public sealed record ProductImageDto(
    int Id,
    string ImageName,
    string ImageUrl,
    string? ThumbnailUrl);
