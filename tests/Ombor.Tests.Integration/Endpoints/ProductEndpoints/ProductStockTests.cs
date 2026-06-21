using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public sealed class ProductStockTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetById_ShouldComputeTotalStockAndValueWeightedAverageCost_AcrossWarehouses()
    {
        // Arrange — same product stocked in two warehouses at different costs.
        var productId = await CreateProductAsync(DefaultCategoryId);
        var warehouseA = await CreateInventoryAsync();
        var warehouseB = await CreateInventoryAsync();
        await SeedStockAsync(warehouseA, productId, quantity: 10, averageCost: 100m);
        await SeedStockAsync(warehouseB, productId, quantity: 30, averageCost: 200m);

        // Act
        var product = await _client.GetAsync<ProductDto>(GetUrl(productId));

        // Assert — totalStock = 40; averageCost = (10*100 + 30*200) / 40 = 175.
        Assert.Equal(40, product.TotalStock);
        Assert.Equal(175m, product.AverageCost);
        Assert.False(product.IsLowStock); // 40 > threshold (10)
        Assert.Equal(2, product.InventoryItems.Length);
        Assert.All(product.InventoryItems, item => Assert.False(string.IsNullOrEmpty(item.InventoryName)));
    }

    [Fact]
    public async Task GetById_ShouldReportZeroStockAndNullAverageCost_WhenNoInventory()
    {
        // Arrange — a product with no inventory items.
        var productId = await CreateProductAsync(DefaultCategoryId);

        // Act
        var product = await _client.GetAsync<ProductDto>(GetUrl(productId));

        // Assert
        Assert.Equal(0, product.TotalStock);
        Assert.Null(product.AverageCost);
        Assert.True(product.IsLowStock); // 0 <= threshold
        Assert.Empty(product.InventoryItems);
    }

    private async Task<int> CreateInventoryAsync()
    {
        var inventory = new Inventory
        {
            Name = $"Warehouse {Guid.NewGuid():N}",
            Location = "Tashkent",
            IsActive = true,
        };
        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();

        return inventory.Id;
    }

    private async Task SeedStockAsync(int inventoryId, int productId, int quantity, decimal averageCost)
    {
        _context.InventoryItems.Add(new InventoryItem
        {
            InventoryId = inventoryId,
            ProductId = productId,
            Quantity = quantity,
            AverageCost = averageCost,
            Inventory = null!,
            Product = null!,
        });
        await _context.SaveChangesAsync();
    }
}
