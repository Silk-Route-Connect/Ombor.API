using Bogus;
using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Generators;

public static class InventoryGenerator
{
    private const string DefaultLocale = "en";
    private const int DefaultMaxItemsCount = 10;

    public static Inventory Generate(int[] productIds, int maxItemsCount = DefaultMaxItemsCount, string? locale = DefaultLocale)
        => GetGenerator(productIds, maxItemsCount, locale).Generate();

    public static List<Inventory> Generate(int[] productIds, int maxItemsCount = DefaultMaxItemsCount, int count = 5, string? locale = DefaultLocale)
        => GetGenerator(productIds, maxItemsCount, locale).Generate(count);

    private static Faker<Inventory> GetGenerator(int[] productIds, int maxItemsCount = DefaultMaxItemsCount, string? locale = DefaultLocale) => new Faker<Inventory>(locale)
        .RuleFor(x => x.Name, f => $"{f.Address.City()} Inventory {f.Random.Number(1, 99)}")
        .RuleFor(x => x.Location, f => f.Address.StreetAddress())
        .RuleFor(x => x.IsActive, f => f.Random.Bool())
        .RuleFor(x => x.InventoryItems, _ => GetItems(productIds, maxItemsCount));

    private static List<InventoryItem> GetItems(int[] productIds, int maxItemsCount) => new Faker<InventoryItem>()
        .RuleFor(x => x.Quantity, f => f.Random.Number(1, 100))
        .RuleFor(x => x.ProductId, f => f.PickRandom(productIds))
        .GenerateBetween(1, maxItemsCount);
}
