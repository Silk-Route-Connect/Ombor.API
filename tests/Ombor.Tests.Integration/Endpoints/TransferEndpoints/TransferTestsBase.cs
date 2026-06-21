using Ombor.Contracts.Responses.Transfer;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TransferEndpoints;

public abstract class TransferTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper output)
        : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Transfer;
    protected override string GetUrl(int id) => $"{Routes.Transfer}/{id}";

    protected async Task<int> CreateWarehouseAsync()
    {
        var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent" };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return warehouse.Id;
    }

    protected async Task<int> CreateProductAsync()
    {
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            SalePrice = 100m,
            SupplyPrice = 50m,
            RetailPrice = 90m,
            LowStockThreshold = 10,
            Measurement = UnitOfMeasurement.Unit,
            Type = ProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }

    protected async Task SeedStockAsync(int warehouseId, int productId, int quantity, decimal averageCost = 50m)
    {
        var item = new WarehouseItem
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            AverageCost = averageCost,
            Warehouse = null!,
            Product = null!,
        };
        _context.WarehouseItems.Add(item);
        await _context.SaveChangesAsync();
    }

    protected Task<TransferDto> PostTransferAsync(int fromWarehouseId, int toWarehouseId, int productId, int quantity)
        => _client.PostAsync<TransferDto>(
            Routes.Transfer,
            new
            {
                fromWarehouseId,
                toWarehouseId,
                note = (string?)null,
                lines = new[] { new { productId, quantity } },
            });
}
