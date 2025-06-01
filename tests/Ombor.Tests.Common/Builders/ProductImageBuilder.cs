using Bogus;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Generators;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class ProductImageBuilder(Faker faker) : BuilderBase(faker), IProductImageBuilder
{
    private int? _id;
    private string? _fileName;
    private string? _imageName;
    private string? _originalUrl;
    private string? _thumbnailUrl;
    private int? _productId;
    private Product? _product;

    public IProductImageBuilder WithId(int? id = null)
    {
        _id = id ?? _faker.Random.Number();

        return this;
    }

    public IProductImageBuilder WithFileName(string? fileName = null)
    {
        _fileName = fileName ?? _faker.System.FileName();

        return this;
    }

    public IProductImageBuilder WithImageName(string? imageName = null)
    {
        _imageName = imageName ?? _faker.System.FileName();
        return this;
    }

    public IProductImageBuilder WithOriginalUrl(string? originalUrl = null)
    {
        _originalUrl = originalUrl ?? _faker.Image.PlaceImgUrl();
        return this;
    }

    public IProductImageBuilder WithThumbnailUrl(string? thumbnailUrl = null)
    {
        _thumbnailUrl = thumbnailUrl ?? _faker.Image.PlaceImgUrl();
        return this;
    }

    public IProductImageBuilder WithProductId(int? productId = null)
    {
        _productId = productId ?? _faker.Random.Number();

        return this;
    }

    public IProductImageBuilder WithProduct(Product? product = null)
    {
        var categoryId = product?.Category?.Id ?? _faker.Random.Number();
        _product = product ?? ProductGenerator.Generate(categoryId);

        return this;
    }

    public ProductImage Build() =>
        new()
        {
            Id = _id ?? default,
            FileName = _fileName ?? string.Empty,
            ImageName = _imageName ?? string.Empty,
            OriginalUrl = _originalUrl ?? string.Empty,
            ThumbnailUrl = _thumbnailUrl,
            ProductId = default,
            Product = null!
        };

    public ProductImage BuildAndPopulate()
    {
        var id = _id ?? _faker.Random.Number();
        var product = _product ?? ProductGenerator.Generate(categoryId: 100);
        product.Id = _productId ?? _faker.Random.Number();

        return new()
        {
            Id = _id ?? id,
            FileName = _fileName ?? _faker.System.FileName(),
            ImageName = _imageName ?? _faker.System.CommonFileName(),
            OriginalUrl = _originalUrl ?? _faker.Image.PicsumUrl(),
            ThumbnailUrl = _thumbnailUrl ?? _faker.Image.PicsumUrl(),
            ProductId = product.Id,
            Product = product
        };
    }
}
