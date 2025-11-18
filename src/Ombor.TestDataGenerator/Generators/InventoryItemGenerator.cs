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
        string? locale = DefaultLocale)
    {
        var products = ProductGenerator.Generate(10, productIds.Length);

        products = EnsureProductsMatch(productIds, products);

        return new Faker<InventoryItem>(locale)
            .RuleFor(x => x.Quantity, f => f.Random.Number(1, 100))
            .RuleFor(x => x.ProductId, f => f.PickRandom(productIds))
            .RuleFor(x => x.Product, f => f.PickRandom(products))
            .RuleFor(x => x.InventoryId, f => f.PickRandom(inventoryIds));
    }

    private static List<Product> EnsureProductsMatch(int[] productIds, List<Product> products)
    {
        for (int i = 0; i < products.Count; i++)
        {
            products[i].Id = productIds[i];
        }

        return products;
    }

}
