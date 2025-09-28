using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Extensions;
using Ombor.TestDataGenerator.Helpers;

namespace Ombor.TestDataGenerator.Generators;

public static class ProductGenerator
{
    private const string DefaultLocale = "en";
    private const int MinThousandsExclusive = 10; // 10,000
    private const int MaxThousandsInclusive = 100; // 1,000,000

    public static Product Generate(int categoryId, string locale = DefaultLocale)
        => GetGenerator([categoryId], locale);

    public static Product Generate(int[] categories, string locale = DefaultLocale)
        => GetGenerator(categories, locale).Generate();

    public static List<Product> Generate(int categoryId, int count, string locale = DefaultLocale)
        => GetGenerator([categoryId], locale).Generate(count);

    public static List<Product> Generate(int[] categories, int count, string locale = DefaultLocale)
        => GetGenerator(categories, locale).Generate(count);

    private static Faker<Product> GetGenerator(int[] categories, string locale) => new Faker<Product>(locale)
        .RuleFor(x => x.Name, f => f.Commerce.ProductName())
        .RuleFor(x => x.CategoryId, f => f.PickRandom(categories))
        .RuleFor(x => x.SKU, (_, p) => ProductHelpers.GenerateSku(p.Name, p.CategoryId))
        .RuleFor(x => x.Description, f => f.Commerce.ProductDescription())
        .RuleFor(x => x.Barcode, f => f.Commerce.Ean13())
        .RuleFor(x => x.Type, f => f.Random.Enum<ProductType>())
        .RuleFor(x => x.SupplyPrice, (f, p) =>
        {
            if (p.Type == ProductType.Sale)
            {
                return 0m;
            }

            return f.Random.NextThousand(MinThousandsExclusive + 1, MaxThousandsInclusive);
        })
        .RuleFor(x => x.SalePrice, (f, p) =>
        {
            if (p.Type == ProductType.Supply)
            {
                return 0m;
            }

            var minK = Math.Max(ToThousandsCeil(p.SupplyPrice), MinThousandsExclusive) + 1;
            var maxK = Math.Min(minK + 120, MaxThousandsInclusive);
            return f.Random.NextThousand(minK, maxK);
        })
        .RuleFor(x => x.RetailPrice, (f, p) =>
        {
            if (p.Type == ProductType.Supply)
            {
                return 0m;
            }

            var minK = Math.Max(ToThousandsCeil(p.SupplyPrice), MinThousandsExclusive + 1);
            var maxK = Math.Max(minK, ToThousandsFloor(p.SalePrice));
            return f.Random.NextThousand(minK, maxK);
        })
        .RuleFor(x => x.QuantityInStock, f => f.Random.Number(10, 100))
        .RuleFor(x => x.LowStockThreshold, (f, p) =>
        {
            var max = Math.Min(100, (int)(p.QuantityInStock / 2m));
            return f.Random.Number(10, Math.Max(10, max));
        })
        .RuleFor(x => x.Measurement, f => f.Random.Enum<UnitOfMeasurement>());

    private static int ToThousandsCeil(decimal value) => (int)Math.Ceiling(value / 1000m);
    private static int ToThousandsFloor(decimal value) => (int)Math.Floor(value / 1000m);
}
