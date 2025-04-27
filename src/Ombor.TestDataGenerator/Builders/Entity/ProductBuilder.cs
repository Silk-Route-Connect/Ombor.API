using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Generators.Entities;
using Ombor.TestDataGenerator.Interfaces.Builders.Entity;

namespace Ombor.TestDataGenerator.Builders.Entity;

internal sealed class ProductBuilder(Faker faker) : BuilderBase(faker), IProductBuilder
{
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
    private DateOnly? _expireDate;
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
        _measurement = measurement ?? _faker.PickRandom<UnitOfMeasurement>();

        return this;
    }

    public IProductBuilder WithExpireDate(DateOnly? expireDate = null)
    {
        _expireDate = expireDate ?? _faker.Date.FutureDateOnly();

        return this;
    }

    public IProductBuilder WithCategory(Category? category = null)
    {
        _category = category ?? GetRandomCategory();

        return this;
    }

    public Product Build()
    {
        var category = _category ?? GetRandomCategory();

        return new()
        {
            Id = _id ?? _faker.Random.Number(),
            Name = _name ?? _faker.Commerce.ProductName(),
            SKU = _sku ?? _faker.Random.Guid().ToString(),
            Description = _description,
            Barcode = _barcode,
            SalePrice = _salePrice ?? 0,
            SupplyPrice = _supplyPrice ?? 0,
            RetailPrice = _retailPrice ?? 0,
            QuantityInStock = _quantityInStock ?? 0,
            LowStockThreshold = _lowStockThreshold ?? 0,
            Measurement = _measurement ?? UnitOfMeasurement.None,
            ExpireDate = _expireDate,
            CategoryId = category.Id,
            Category = category
        };
    }

    public Product BuildAndPopulate()
    {
        var category = _category ?? GetRandomCategory();

        return new()
        {
            Id = _id ?? _faker.Random.Number(),
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
            ExpireDate = _expireDate ?? _faker.Date.FutureDateOnly(),
            CategoryId = category.Id,
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
}
