using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Ombor.Application.Services;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public abstract class ProductTestsBase : ServiceTestsBase
{
    protected const string MatchingSearchTerm = "Test Match";
    protected const int MatchingCategoryId = 50;
    protected const decimal MatchingMinPrice = 100m;
    protected const decimal MatchingMaxPrice = 1_000m;

    private protected readonly ProductService _service;
    protected readonly Product[] _defaultProducts;

    protected ProductTestsBase()
    {
        _defaultProducts = CreateRandomProducts();
        SetupProducts(_defaultProducts);

        _service = new ProductService(_mockContext.Object, _mockValidator.Object);
    }

    public static TheoryData<GetProductsRequest> GetRequests =>
        new()
        {
            new GetProductsRequest(null, null, null, null),
            new GetProductsRequest(MatchingSearchTerm, null, null, null),
            new GetProductsRequest(null, MatchingCategoryId, null, null),
            new GetProductsRequest(null, null, MatchingMinPrice, MatchingMaxPrice),
            new GetProductsRequest(MatchingSearchTerm, MatchingCategoryId, MatchingMinPrice, MatchingMaxPrice)
        };

    protected Mock<DbSet<Product>> SetupProducts(IEnumerable<Product> products)
    {
        var mockSet = products.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Products).Returns(mockSet.Object);

        return mockSet;
    }

    protected Product[] CreateRandomProducts(int count = 5)
    {
        var categories = Enumerable.Range(0, count)
            .Select(i => _builder.CategoryBuilder
                .WithId(i + 1)
                .Build());

        return Enumerable.Range(0, count)
            .Select(i => _builder.ProductBuilder
                .WithId(i + 1)
                .WithName()
                .WithDescription()
                .WithSalePrice()
                .WithCategory(_faker.PickRandom(categories))
                .BuildAndPopulate())
            .ToArray();
    }

    protected static bool IsEquivalent(Product product, CreateProductRequest request) =>
        product.Name == request.Name &&
        product.SKU == request.SKU &&
        product.Description == request.Description &&
        product.Barcode == request.Barcode &&
        product.SalePrice == request.SalePrice &&
        product.SupplyPrice == request.SupplyPrice &&
        product.RetailPrice == request.RetailPrice &&
        product.QuantityInStock == request.QuantityInStock &&
        product.LowStockThreshold == request.LowStockThreshold &&
        product.Measurement.ToString() == request.Measurement &&
        product.ExpireDate == request.ExpireDate &&
        product.CategoryId == request.CategoryId;
}
