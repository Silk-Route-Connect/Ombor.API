using Bogus;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Helpers;

namespace Ombor.TestDataGenerator.Generators;

public static class ProductGenerator
{
    private static readonly Random _rng = new();

    public static Product Generate(int[] categories)
        => GetGenerator(categories).Generate();

    public static IEnumerable<Product> Generate(int[] categories, int count)
        => GetGenerator(categories).Generate(count);

    private static Faker<Product> GetGenerator(int[] categories) => new Faker<Product>("ru")
        .RuleFor(x => x.Name, f => f.Commerce.ProductName())
        .RuleFor(x => x.CategoryId, f => f.PickRandom(categories))
        .RuleFor(x => x.SKU, (_, p) => ProductHelpers.GenerateSku(p.Name, p.CategoryId))
        .RuleFor(x => x.Description, f => f.Commerce.ProductDescription())
        .RuleFor(x => x.Barcode, f => f.Commerce.Ean13())
        .RuleFor(x => x.SupplyPrice, f => f.Random.Decimal(10_000, 1_000_000))
        .RuleFor(x => x.SalePrice, (f, p) => f.Random.Decimal(p.SupplyPrice + 10_000, p.SupplyPrice + 50_000))
        .RuleFor(x => x.RetailPrice, (f, p) => f.Random.Decimal(p.SupplyPrice + 2_000, p.SupplyPrice + 10_000))
        .RuleFor(x => x.QuantityInStock, f => f.Random.Number(10, 10_000))
        .RuleFor(x => x.LowStockThreshold, f => f.Random.Number(10, 100))
        .RuleFor(x => x.Measurement, f => f.Random.Enum<UnitOfMeasurement>())
        .RuleFor(x => x.ExpireDate, f => f.Date.FutureDateOnly());

    public static CreateProductRequest GenerateCreateRequest()
    {
        var entity = Generate([1, 2, 3]);

        return new CreateProductRequest(
            CategoryId: entity.CategoryId,
            Name: entity.Name,
            SKU: entity.SKU,
            Measurement: entity.Measurement.ToString(),
            Description: entity.Description,
            Barcode: entity.Barcode,
            SalePrice: entity.SalePrice,
            SupplyPrice: entity.SupplyPrice,
            RetailPrice: entity.RetailPrice,
            QuantityInStock: entity.QuantityInStock,
            LowStockThreshold: entity.LowStockThreshold,
            ExpireDate: entity.ExpireDate);
    }

    public static UpdateProductRequest GenerateUpdateRequest()
    {
        var entity = Generate([1, 2, 3]);

        return new UpdateProductRequest(
            Id: _rng.Next(),
            CategoryId: entity.CategoryId,
            Name: entity.Name,
            SKU: entity.SKU,
            Measurement: entity.Measurement.ToString(),
            Description: entity.Description,
            Barcode: entity.Barcode,
            SalePrice: entity.SalePrice,
            SupplyPrice: entity.SupplyPrice,
            RetailPrice: entity.RetailPrice,
            QuantityInStock: entity.QuantityInStock,
            LowStockThreshold: entity.LowStockThreshold,
            ExpireDate: entity.ExpireDate);
    }
}
