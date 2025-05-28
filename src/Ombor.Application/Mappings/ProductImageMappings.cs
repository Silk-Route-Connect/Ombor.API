using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class ProductImageMappings
{
    public static ProductImageDto[] ToDto(this IEnumerable<ProductImage> images)
        => [.. images.Select(ToDto)];

    public static ProductImageDto ToDto(this ProductImage image)
        => new(
            Id: image.Id,
            Name: image.Name,
            OriginalUrl: image.OriginalUrl,
            ThumbnailUrl: image.ThumbnailUrl);
}
