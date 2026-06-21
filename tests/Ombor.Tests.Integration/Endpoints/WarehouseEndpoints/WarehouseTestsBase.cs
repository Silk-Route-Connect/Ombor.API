using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

public abstract class WarehouseTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : EndpointTestsBase(factory, outputHelper)
{
    protected readonly string _searchTerm = "Warehouse 123";

    protected override string GetUrl()
        => Routes.Warehouse;

    protected override string GetUrl(int id)
        => $"{Routes.Warehouse}/{id}";

    protected async Task<Warehouse> CreateWarehouseAsync()
    {
        var warehouse = new Warehouse
        {
            Name = "Warehouse 123",
            Location = "Tashkent",
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return warehouse;
    }

    protected async Task<int> CreateWarehouseAsync(Warehouse warehouse)
    {
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return warehouse.Id;
    }

    protected async Task<Product> CreateProductAsync()
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

        return product;
    }
}
