using Bogus;
using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Generators;

public static class WarehouseGenerator
{
    private const string DefaultLocale = "en";
    private const int DefaultMaxItemsCount = 10;

    public static Warehouse Generate(int[] productIds, int maxItemsCount = DefaultMaxItemsCount, string? locale = DefaultLocale)
        => GetGenerator(productIds, maxItemsCount, locale).Generate();

    public static List<Warehouse> Generate(int[] productIds, int maxItemsCount = DefaultMaxItemsCount, int count = 5, string? locale = DefaultLocale)
        => GetGenerator(productIds, maxItemsCount, locale).Generate(count);

    private static Faker<Warehouse> GetGenerator(int[] productIds, int maxItemsCount = DefaultMaxItemsCount, string? locale = DefaultLocale) => new Faker<Warehouse>(locale)
        .RuleFor(x => x.Name, f => $"{f.Address.City()} Warehouse {f.Random.Number(1, 99)}")
        .RuleFor(x => x.Location, f => f.Address.StreetAddress())
        .RuleFor(x => x.IsArchived, f => f.Random.Bool(0.15f))
        .RuleFor(x => x.WarehouseItems, _ => GetItems(productIds, maxItemsCount));

    private static List<WarehouseItem> GetItems(int[] productIds, int maxItemsCount) => new Faker<WarehouseItem>()
        .RuleFor(x => x.Quantity, f => f.Random.Number(1, 100))
        .RuleFor(x => x.ProductId, f => f.PickRandom(productIds))
        .GenerateBetween(1, maxItemsCount);
}
