using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Helpers;

namespace Ombor.TestDataGenerator.Generators;

public static class ProductGenerator
{
    private const string DefaultLocale = "en";

    public static Product Generate(int categoryId, string locale = DefaultLocale)
        => GetGenerator([categoryId], locale);

    public static Product Generate(int[] categories, string locale = DefaultLocale)
        => GetGenerator(categories, locale).Generate();

    public static List<Product> Generate(int categoryId, int count, string locale = DefaultLocale)
        => GetGenerator([categoryId], locale).Generate(count);

    public static List<Product> Generate(int[] categories, int count, string locale = DefaultLocale)
        => GetGenerator(categories, locale).Generate(count);

    public static List<ProductImage> GenerateImages(int count, string locale = DefaultLocale)
        => GenerateImage(locale).Generate(count);

    public static List<ProductImage> GenerateImages(int count, int productId, string locale = DefaultLocale)
        => GenerateImage(productId, locale).Generate(count);

    private static Faker<Product> GetGenerator(int[] categories, string locale) => new Faker<Product>(locale)
        .RuleFor(x => x.Name, f => f.Commerce.ProductName())
        .RuleFor(x => x.CategoryId, f => f.PickRandom(categories))
        .RuleFor(x => x.SKU, (_, p) => ProductHelpers.GenerateSku(p.Name, p.CategoryId))
        .RuleFor(x => x.Description, f => f.Commerce.ProductDescription())
        .RuleFor(x => x.Barcode, f => f.Commerce.Ean13())
        .RuleFor(x => x.Type, f => f.Random.Enum<ProductType>())
        .RuleFor(x => x.SupplyPrice, (f, p) => p.Type != ProductType.Sale ? f.Random.Decimal(10_000, 1_000_000) : 0)
        .RuleFor(x => x.SalePrice, (f, p) => p.Type != ProductType.Supply ? f.Random.Decimal(p.SupplyPrice + 10_000, p.SupplyPrice + 50_000) : 0)
        .RuleFor(x => x.RetailPrice, (f, p) => p.Type != ProductType.Supply ? f.Random.Decimal(p.SupplyPrice + 2_000, p.SupplyPrice + 10_000) : 0)
        .RuleFor(x => x.QuantityInStock, f => f.Random.Number(10, 10_000))
        .RuleFor(x => x.LowStockThreshold, f => f.Random.Number(10, 100))
        .RuleFor(x => x.Measurement, f => f.Random.Enum<UnitOfMeasurement>())
        .RuleFor(x => x.Type, f => f.Random.Enum<ProductType>());

    private static Faker<ProductImage> GenerateImage(string locale) => new Faker<ProductImage>(locale)
        .RuleFor(x => x.ImageName, f => f.System.FileName())
        .RuleFor(x => x.OriginalUrl, f => f.Image.PicsumUrl())
        .RuleFor(x => x.ThumbnailUrl, f => f.Image.PicsumUrl());

    private static Faker<ProductImage> GenerateImage(int productId, string locale)
        => GenerateImage(locale).RuleFor(x => x.ProductId, productId);
}
