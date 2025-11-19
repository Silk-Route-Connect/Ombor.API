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

        EnsureProductsMatch(productIds, products);

        return new Faker<InventoryItem>(locale)
            .RuleFor(x => x.Quantity, f => f.Random.Number(1, 100))
            .RuleFor(x => x.Product, f => f.PickRandom(products))
            .RuleFor(x => x.ProductId, (_, item) => item.Product.Id)
            .RuleFor(x => x.InventoryId, f => f.PickRandom(inventoryIds));
    }

    private static void EnsureProductsMatch(int[] productIds, List<Product> products)
    {
        if (products.Count != productIds.Length)
        {
            throw new ArgumentException("productIds length must match products count.");
        }

        for (int i = 0; i < products.Count; i++)
        {
            products[i].Id = productIds[i];
        }
    }

}
