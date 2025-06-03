using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Interfaces;

public interface IProductImageBuilder
{
    IProductImageBuilder WithId(int? id = null);
    IProductImageBuilder WithFileName(string? fileName = null);
    IProductImageBuilder WithImageName(string? imageName = null);
    IProductImageBuilder WithOriginalUrl(string? originalUrl = null);
    IProductImageBuilder WithThumbnailUrl(string? thumbnailUrl = null);
    IProductImageBuilder WithProductId(int? productId = null);
    IProductImageBuilder WithProduct(Product? product = null);
    ProductImage Build();
    ProductImage BuildAndPopulate();
}
