using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class ProductTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestsBase(factory, outputHelper)
{
    protected const int DefaultCategoryId = 5;

    protected override string GetUrl()
        => Routes.Product;

    protected override string GetUrl(int id)
        => $"{Routes.Product}/{id}";

    protected async Task<int> CreateProductAsync(int categoryId)
    {
        var product = new Product
        {
            Name = "Test Product",
            SKU = $"Test SKU {Guid.NewGuid()}",
            Barcode = "Test Barcode",
            Description = "Test Product Description",
            ExpireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            SalePrice = 100.00m,
            SupplyPrice = 50.00m,
            RetailPrice = 90.00m,
            LowStockThreshold = 10,
            QuantityInStock = 100,
            Measurement = UnitOfMeasurement.Unit,
            CategoryId = categoryId,
            Category = null!
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }

    protected async Task<int> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }
}
