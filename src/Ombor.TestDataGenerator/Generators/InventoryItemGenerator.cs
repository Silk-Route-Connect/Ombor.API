using Bogus;
using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Generators;

public static class InventoryItemGenerator
{
    private const string DefaultLocale = "en";
    public static InventoryItem Generate(int productId, int inventoryId) =>
        GetGenerator([productId], [inventoryId]).Generate();

    public static List<InventoryItem> Generate(int productId, int inventoryId, int itemsCount) =>
    GetGenerator([productId], [inventoryId]).Generate(itemsCount);

    private static Faker<InventoryItem> GetGenerator(
        int[] productIds,
        int[] inventoryIds,
        string? loacle = DefaultLocale)
        => new Faker<InventoryItem>(loacle)
        .RuleFor(x => x.Quantity, f => f.Random.Number(1, 100))
        .RuleFor(x => x.ProductId, f => f.PickRandom(productIds))
        .RuleFor(x => x.InventoryId, f => f.PickRandom(inventoryIds));
}
