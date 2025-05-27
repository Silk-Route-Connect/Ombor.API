using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class GetProductsTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    private const string _matchingSearchTerm = "Test Product";
    private const int _matchingCategoryId = 4;
    private const decimal _minPrice = 10.00m;
    private const decimal _maxPrice = 100.00m;

    [Fact]
    public async Task GetAsync_ShouldReturnFilteredProducts_WhenQueryParametersAreProvided()
    {
        // Arrange
        var request = new GetProductsRequest(_matchingSearchTerm, _matchingCategoryId, _minPrice, _maxPrice, Contracts.Enums.ProductType.All);
        await CreateProductsAsync(request);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<ProductDto[]>(url);

        // Assert
        await _responseValidator.Product.ValidateGetAsync(request, response);
    }

    protected async Task CreateProductsAsync(GetProductsRequest request)
    {
        var searchTerm = request.SearchTerm ?? _matchingSearchTerm;
        var minPrice = request.MinPrice ?? _minPrice;
        var maxPrice = request.MaxPrice ?? _maxPrice;
        var categoryId = request.CategoryId ?? _matchingCategoryId;

        var products = new List<Product>
        {
            // Matching search term by name
            new()
            {
                Name = searchTerm,
                SKU = "SKU1",
                Barcode = "Barcode1",
                Description = "Test Product Description 1",
                SalePrice = 50.00m,
                SupplyPrice = 25.00m,
                RetailPrice = 45.00m,
                LowStockThreshold = 10,
                QuantityInStock = 100,
                Measurement = UnitOfMeasurement.Unit,
                CategoryId = request.CategoryId!.Value,
                Category = null!
            },
            // Matching search term by description
            new()
            {
                Name = "Test Product 2",
                SKU = "SKU2",
                Barcode = "Barcode2",
                Description = searchTerm,
                SalePrice = 60.00m,
                SupplyPrice = 30.00m,
                RetailPrice = 55.00m,
                LowStockThreshold = 10,
                QuantityInStock = 100,
                Measurement = UnitOfMeasurement.Unit,
                CategoryId = request.CategoryId!.Value,
                Category = null!
            },
            // Matching search term by SKU
            new()
            {
                Name = "Test Product 3",
                SKU = searchTerm,
                Barcode = "Barcode3",
                Description = "Test Product Description 3",
                SalePrice = 70.00m,
                SupplyPrice = 35.00m,
                RetailPrice = 65.00m,
                LowStockThreshold = 10,
                QuantityInStock = 100,
                Measurement = UnitOfMeasurement.Unit,
                CategoryId = request.CategoryId!.Value,
                Category = null!
            },
            // Matching search term by min price
            new()
            {
                Name = "Test Product 4",
                SKU = "SKU4",
                Barcode = "Barcode4",
                Description = "Test Product Description 4",
                SalePrice = minPrice,
                SupplyPrice = 40.00m,
                RetailPrice = 75.00m,
                LowStockThreshold = 10,
                QuantityInStock = 100,
                Measurement = UnitOfMeasurement.Unit,
                CategoryId = request.CategoryId!.Value,
                Category = null!
            },
            // Matching search term by max price
            new()
            {
                Name = "Test Product 5",
                SKU = "SKU5",
                Barcode = "Barcode5",
                Description = "Test Product Description 5",
                SalePrice = maxPrice,
                SupplyPrice = 45.00m,
                RetailPrice = 85.00m,
                LowStockThreshold = 10,
                QuantityInStock = 100,
                Measurement = UnitOfMeasurement.Unit,
                CategoryId = request.CategoryId!.Value,
                Category = null!
            },
            // Matching search term by category
            new()
            {
                Name = "Test Product 6",
                SKU = "SKU6",
                Barcode = "Barcode6",
                Description = "Test Product Description 6",
                SalePrice = 80.00m,
                SupplyPrice = 50.00m,
                RetailPrice = 90.00m,
                LowStockThreshold = 10,
                QuantityInStock = 100,
                Measurement = UnitOfMeasurement.Unit,
                CategoryId = categoryId,
                Category = null!
            }
        };

        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();
    }
}
