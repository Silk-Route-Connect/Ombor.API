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
        var warehouseA = await CreateWarehouseAsync();
        var warehouseB = await CreateWarehouseAsync();
        await SeedStockAsync(warehouseA, productId, quantity: 10, averageCost: 100m);
        await SeedStockAsync(warehouseB, productId, quantity: 30, averageCost: 200m);

        // Act
        var product = await _client.GetAsync<ProductDto>(GetUrl(productId));

        // Assert — totalStock = 40; averageCost = (10*100 + 30*200) / 40 = 175.
        Assert.Equal(40, product.TotalStock);
        Assert.Equal(175m, product.AverageCost);
        Assert.False(product.IsLowStock); // 40 > threshold (10)
        Assert.Equal(2, product.WarehouseItems.Length);
        Assert.All(product.WarehouseItems, item => Assert.False(string.IsNullOrEmpty(item.WarehouseName)));
    }

    [Fact]
    public async Task GetById_ShouldReportZeroStockAndNullAverageCost_WhenNoWarehouse()
    {
        // Arrange — a product with no warehouse items.
        var productId = await CreateProductAsync(DefaultCategoryId);

        // Act
        var product = await _client.GetAsync<ProductDto>(GetUrl(productId));

        // Assert
        Assert.Equal(0, product.TotalStock);
        Assert.Null(product.AverageCost);
        Assert.True(product.IsLowStock); // 0 <= threshold
        Assert.Empty(product.WarehouseItems);
    }

    private async Task<int> CreateWarehouseAsync()
    {
        var warehouse = new Warehouse
        {
            Name = $"Warehouse {Guid.NewGuid():N}",
            Location = "Tashkent",
        };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return warehouse.Id;
    }

    private async Task SeedStockAsync(int warehouseId, int productId, int quantity, decimal averageCost)
    {
        _context.WarehouseItems.Add(new WarehouseItem
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            AverageCost = averageCost,
            Warehouse = null!,
            Product = null!,
        });
        await _context.SaveChangesAsync();
    }
}
