using Bogus;
using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Generators;

public static class WarehouseItemGenerator
{
    private const string DefaultLocale = "en";
    public static WarehouseItem Generate(int productId, int warehouseId) =>
        GetGenerator([productId], [warehouseId]).Generate();

    public static List<WarehouseItem> Generate(int productId, int warehouseId, int itemsCount) =>
    GetGenerator([productId], [warehouseId]).Generate(itemsCount);

    private static Faker<WarehouseItem> GetGenerator(
        int[] productIds,
        int[] warehouseIds,
        string? loacle = DefaultLocale)
        => new Faker<WarehouseItem>(loacle)
        .RuleFor(x => x.Quantity, f => f.Random.Number(1, 100))
        .RuleFor(x => x.ProductId, f => f.PickRandom(productIds))
        .RuleFor(x => x.WarehouseId, f => f.PickRandom(warehouseIds));
}
