using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Generators;
using Ombor.Tests.Common.Configurations;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

/// <summary>
/// <inheritdoc/>
/// </summary>
internal sealed class ProductBuilder(Faker faker) : BuilderBase(faker), IProductBuilder
{
    private const int DefaultImagesCount = 5;

    private int? _id;
    private string? _name;
    private string? _sku;
    private string? _description;
    private string? _barcode;
    private decimal? _salePrice;
    private decimal? _supplyPrice;
    private decimal? _retailPrice;
    private int? _quantityInStock;
    private int? _lowStockThreshold;
    private UnitOfMeasurement? _measurement;
    private ProductType? _type;
    private List<ProductImage>? _images;
    private int? _categoryId;
    private Category? _category;

    public IProductBuilder WithId(int? id = null)
    {
        _id = id ?? _faker.Random.Number();

        return this;
    }

    public IProductBuilder WithName(string? name = null)
    {
        _name = name ?? _faker.Commerce.ProductName();

        return this;
    }

    public IProductBuilder WithSKU(string? sku = null)
    {
        _sku = sku ?? _faker.Random.Guid().ToString();

        return this;
    }

    public IProductBuilder WithDescription(string? description = null)
    {
        _description = description ?? _faker.Commerce.ProductDescription();

        return this;
    }

    public IProductBuilder WithBarcode(string? barcode = null)
    {
        _barcode = barcode ?? _faker.Commerce.Ean13();

        return this;
    }

    public IProductBuilder WithSalePrice(decimal? salePrice = null)
    {
        _salePrice = salePrice ?? GetRandomPrice();

        return this;
    }

    public IProductBuilder WithSupplyPrice(decimal? supplyPrice = null)
    {
        _supplyPrice = supplyPrice ?? GetRandomPrice();

        return this;
    }

    public IProductBuilder WithRetailPrice(decimal? retailPrice = null)
    {
        _retailPrice = retailPrice ?? GetRandomPrice();

        return this;
    }

    public IProductBuilder WithQuantityInStock(int? quantityInStock = null)
    {
        _quantityInStock = quantityInStock ?? GetRandomStockAmount();

        return this;
    }

    public IProductBuilder WithLowStockThreshold(int? lowStockThreshold = null)
    {
        _lowStockThreshold = lowStockThreshold ?? GetRandomLowStockThresholdAmount();

        return this;
    }

    public IProductBuilder WithMeasurement(UnitOfMeasurement? measurement = null)
    {
        _measurement = measurement ?? _faker.Random.Enum<UnitOfMeasurement>();

        return this;
    }

    public IProductBuilder WithType(ProductType? type = null)
    {
        _type = type ?? _faker.Random.Enum<ProductType>();

        return this;
    }

    public IProductBuilder WithImages(IEnumerable<ProductImage>? images = null)
    {
        var productIdForImages = _id ?? _faker.Random.Number();

        _images = images is null
            ? GetProductImages(productIdForImages, DefaultImagesCount)
            : [.. images];

        return this;
    }

    public IProductBuilder WithCategory(Category? category = null)
    {
        category ??= GetRandomCategory();
        _category = category;
        _categoryId = category.Id;

        return this;
    }

    public IProductBuilder WithCategoryId(int? categoryId = null)
    {
        _categoryId = categoryId ?? _faker.Random.Number();

        return this;
    }

    public Product Build()
    {
        var category = _category ?? GetRandomCategory();
        var categoryId = _categoryId ?? category.Id;

        return new()
        {
            Id = _id ?? default,
            Name = _name ?? string.Empty,
            SKU = _sku ?? string.Empty,
            Description = _description ?? default,
            Barcode = _barcode ?? default,
            SalePrice = _salePrice ?? default,
            SupplyPrice = _supplyPrice ?? default,
            RetailPrice = _retailPrice ?? default,
            QuantityInStock = _quantityInStock ?? default,
            LowStockThreshold = _lowStockThreshold ?? default,
            Measurement = _measurement ?? UnitOfMeasurement.None,
            Type = _type ?? ProductType.All,
            Images = _images ?? [],
            CategoryId = categoryId,
            Category = category,
        };
    }

    public Product BuildAndPopulate()
    {
        var id = _id ?? _faker.Random.Number();
        var category = _category ?? GetRandomCategory();
        var categoryId = _categoryId ?? category.Id;
        var images = _images ?? GetProductImages(id, DefaultImagesCount);

        return new()
        {
            Id = id,
            Name = _name ?? _faker.Commerce.ProductName(),
            SKU = _sku ?? _faker.Random.Guid().ToString(),
            Description = _description ?? _faker.Lorem.Sentence(1),
            Barcode = _barcode ?? _faker.Commerce.Ean13(),
            SalePrice = _salePrice ?? GetRandomPrice(),
            SupplyPrice = _supplyPrice ?? GetRandomPrice(),
            RetailPrice = _retailPrice ?? GetRandomPrice(),
            QuantityInStock = _quantityInStock ?? GetRandomStockAmount(),
            LowStockThreshold = _lowStockThreshold ?? GetRandomLowStockThresholdAmount(),
            Measurement = _measurement ?? _faker.Random.Enum<UnitOfMeasurement>(),
            Type = _type ?? _faker.Random.Enum<ProductType>(),
            Images = [.. images],
            CategoryId = categoryId,
            Category = category
        };
    }

    private Category GetRandomCategory()
    {
        var category = CategoryGenerator.Generate();
        category.Id = _faker.Random.Number();

        return category;
    }

    private decimal GetRandomPrice() =>
        _faker.Finance.Amount(BuilderConstants.MinPriceAmount, BuilderConstants.MaxPriceAmount);

    private int GetRandomStockAmount() =>
        _faker.Random.Number(BuilderConstants.MinStockAmount, BuilderConstants.MaxStockAmount);

    private int GetRandomLowStockThresholdAmount() =>
        _faker.Random.Number(BuilderConstants.MinLowThresholdAmount, BuilderConstants.MaxLowThresholdAmount);

    private List<ProductImage> GetProductImages(int productId, int count) => Enumerable.Range(1, count + 1)
        .Select(i => new ProductImage
        {
            Id = i,
            FileName = _faker.System.FilePath(),
            ImageName = _faker.System.CommonFileName(),
            OriginalUrl = _faker.Image.PicsumUrl(),
            ThumbnailUrl = _faker.Image.PicsumUrl(),
            ProductId = productId,
            Product = null!
        })
        .ToList();
}
