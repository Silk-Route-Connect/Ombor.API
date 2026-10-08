using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainEnums = Ombor.Domain.Enums;

namespace Ombor.Tests.Integration.Endpoints.NotificationEndpoints;

/// <summary>
/// The <c>LowStock</c> alert (DR-41): one item per tracked warehouse item at or below its own threshold, naming its
/// warehouse, the largest shortage first.
/// </summary>
public sealed class LowStockNotificationTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : NotificationTestsBase(factory, output)
{
    [Fact]
    public async Task LowStock_IsOneItemPerTrackedWarehouseItem_WithItsWarehouse_LargestShortageFirst()
    {
        var before = await GetAsync(NotificationKind.LowStock);
        var main = await AddWarehouseAsync(_context, archived: false);
        var branch = await AddWarehouseAsync(_context, archived: false);

        // Counted, once per warehouse: the same product low in two warehouses — the largest shortages there are — and
        // a sold-out product tracked in the main warehouse.
        var product = await AddProductAsync(_context, archived: false);
        await AddItemAsync(_context, main.Id, product.Id, quantity: 2m, threshold: 1_000_000m);
        await AddItemAsync(_context, branch.Id, product.Id, quantity: 3m, threshold: 1_000_000m);
        var soldOut = await AddProductAsync(_context, archived: false);
        await AddItemAsync(_context, main.Id, soldOut.Id, quantity: 0m, threshold: 5m);

        // Not counted: above its threshold, not tracked (even at zero), an archived product, an archived warehouse,
        // and another organization's low item.
        await AddItemAsync(_context, main.Id, (await AddProductAsync(_context, archived: false)).Id, quantity: 6m, threshold: 5m);
        await AddItemAsync(_context, main.Id, (await AddProductAsync(_context, archived: false)).Id, quantity: 0m, threshold: null);
        await AddItemAsync(_context, main.Id, (await AddProductAsync(_context, archived: true)).Id, quantity: 0m, threshold: 5m);
        var archivedWarehouse = await AddWarehouseAsync(_context, archived: true);
        await AddItemAsync(_context, archivedWarehouse.Id, (await AddProductAsync(_context, archived: false)).Id, quantity: 1m, threshold: 5m);
        await using (var foreign = CreateContext(ForeignOrganizationId))
        {
            var foreignWarehouse = await AddWarehouseAsync(foreign, archived: false);
            await AddItemAsync(foreign, foreignWarehouse.Id, (await AddProductAsync(foreign, archived: false)).Id, quantity: 0m, threshold: 2_000_000m);
        }

        var after = await GetAsync(NotificationKind.LowStock);

        Assert.NotNull(after);
        Assert.Equal(NotificationSeverity.Warning, after.Severity);
        Assert.Equal((before?.Count ?? 0) + 3, after.Count);
        Assert.Null(after.Amount);
        Assert.True(after.Items.Length <= 10);

        var first = after.Items[0];
        Assert.Equal(ActivityEntityKind.Product, first.EntityKind);
        Assert.Equal(product.Id, first.Id);
        Assert.Equal(product.Name, first.Label);
        Assert.Equal(product.SKU, first.Detail);
        Assert.Equal(2m, first.Quantity);
        Assert.Equal(1_000_000m, first.Threshold);
        Assert.Equal("Piece", first.Measurement);
        Assert.Equal(main.Id, first.WarehouseId);
        Assert.Equal(main.Name, first.WarehouseName);

        var second = after.Items[1];
        Assert.Equal(product.Id, second.Id);
        Assert.Equal(3m, second.Quantity);
        Assert.Equal(branch.Id, second.WarehouseId);
        Assert.Equal(branch.Name, second.WarehouseName);
    }

    [Fact]
    public async Task LowStock_EqualShortages_AreOrderedByProductName_ThenByWarehouseName()
    {
        var prefix = $"Tie {Guid.NewGuid():N}";
        var warehouseA = await AddWarehouseAsync(_context, archived: false, $"{prefix} A");
        var warehouseB = await AddWarehouseAsync(_context, archived: false, $"{prefix} B");
        var product1 = await AddProductAsync(_context, archived: false, $"{prefix} 1");
        var product2 = await AddProductAsync(_context, archived: false, $"{prefix} 2");

        // Planted out of order, all four short by the same amount. It is below the largest-shortage test's, so these rows
        // never push that test's rows off the top, yet above every other test's, so all four stay in the top ten.
        const decimal threshold = 500_000m;
        await AddItemAsync(_context, warehouseB.Id, product2.Id, quantity: 0m, threshold);
        await AddItemAsync(_context, warehouseA.Id, product2.Id, quantity: 0m, threshold);
        await AddItemAsync(_context, warehouseB.Id, product1.Id, quantity: 0m, threshold);
        await AddItemAsync(_context, warehouseA.Id, product1.Id, quantity: 0m, threshold);

        var after = await GetAsync(NotificationKind.LowStock);

        Assert.NotNull(after);
        var planted = after.Items
            .Where(i => i.Label?.StartsWith(prefix, StringComparison.Ordinal) == true)
            .Select(i => (i.Id, i.WarehouseId))
            .ToArray();
        Assert.Equal(
            [(product1.Id, warehouseA.Id), (product1.Id, warehouseB.Id), (product2.Id, warehouseA.Id), (product2.Id, warehouseB.Id)],
            planted);
    }

    private static async Task<Warehouse> AddWarehouseAsync(IApplicationDbContext context, bool archived, string? name = null)
    {
        var warehouse = new Warehouse { Name = name ?? $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent", IsArchived = archived };
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        return warehouse;
    }

    private static async Task<Product> AddProductAsync(IApplicationDbContext context, bool archived, string? name = null)
    {
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = name ?? $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            SalePrice = 100m,
            SupplyPrice = 50m,
            RetailPrice = 90m,
            IsArchived = archived,
            Measurement = DomainEnums.UnitOfMeasurement.Piece,
            Type = DomainEnums.ProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return product;
    }

    private static async Task AddItemAsync(
        IApplicationDbContext context, int warehouseId, int productId, decimal quantity, decimal? threshold)
    {
        context.WarehouseItems.Add(new WarehouseItem
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            AverageCost = 50m,
            LowStockThreshold = threshold,
            Warehouse = null!,
            Product = null!,
        });
        await context.SaveChangesAsync();
    }
}
