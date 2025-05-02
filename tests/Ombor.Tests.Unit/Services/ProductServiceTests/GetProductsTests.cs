using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class GetProductsTests : ProductTestsBase
{
    private const string MatchingSearchTerm = "Test Match";
    private const int MatchingCategoryId = 50;
    private const decimal MatchingMinPrice = 100m;
    private const decimal MatchingMaxPrice = 1_000m;

    public static TheoryData<GetProductsRequest> GetRequests =>
        new()
        {
            new GetProductsRequest(null, null, null, null),
            new GetProductsRequest(string.Empty, null, null, null),
            new GetProductsRequest(" ", null, null, null),
            new GetProductsRequest("   ", null, null, null),
            new GetProductsRequest(MatchingSearchTerm, null, null, null),
            new GetProductsRequest(null, MatchingCategoryId, null, null),
            new GetProductsRequest(null, null, MatchingMinPrice, MatchingMaxPrice),
            new GetProductsRequest(MatchingSearchTerm, MatchingCategoryId, MatchingMinPrice, MatchingMaxPrice)
        };

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetProductsRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoProducts()
    {
        // Arrange
        var request = new GetProductsRequest(string.Empty, null, null, null);
        SetupProducts([]);

        // Act
        var actual = await _service.GetAsync(request);

        // Assert
        Assert.Empty(actual);
    }

    [Theory]
    [MemberData(nameof(GetRequests))]
    public async Task GetAsync_ShouldReturnMatchingProducts(GetProductsRequest request)
    {
        // Arrange
        var matchingProducts = CreateMatchingProducts(request);
        Product[] allProducts = [.. _defaultProducts, .. matchingProducts];
        var expectedProducts = request.IsEmpty()
            ? allProducts
            : matchingProducts;

        SetupProducts(allProducts);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(expectedProducts.Length, response.Length);
        Assert.All(response, actual =>
        {
            var expected = expectedProducts.SingleOrDefault(x => x.Id == actual.Id);

            ProductAssertionHelper.AssertEquivalent(expected, actual);
        });
    }

    private Product[] CreateMatchingProducts(GetProductsRequest request)
    {
        var products = new List<Product>();

        if (request.IsFullyPopulated())
        {
            return CreateProductsMatchingAllFilters(request);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            products.AddRange(CreateProductsMatchingSearch(request.SearchTerm));
        }

        if (request.CategoryId.HasValue)
        {
            products.AddRange(CreateProductsMatchingCategory(request.CategoryId.Value));
        }

        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            var minPrice = request.MinPrice ?? 0;
            var maxPrice = request.MaxPrice ?? decimal.MaxValue;
            products.AddRange(CreateProductsMatchingPriceRange(minPrice, maxPrice));
        }

        return products.ToArray();
    }

    private Product[] CreateProductsMatchingSearch(string searchTerm)
    {
        var productMatchingName = _builder.ProductBuilder
            .WithId(100)
            .WithName(searchTerm)
            .WithDescription()
            .WithCategory()
            .Build();
        var matchingDescription = _builder.ProductBuilder
            .WithId(101)
            .WithName()
            .WithDescription(searchTerm)
            .WithCategory()
            .Build();

        return [productMatchingName, matchingDescription];
    }

    private Product[] CreateProductsMatchingCategory(int categoryId)
    {
        var category = _builder.CategoryBuilder
            .WithId(categoryId)
            .Build();

        var productMatchingCategory = _builder.ProductBuilder
            .WithId(102)
            .WithCategory(category)
            .Build();

        return [productMatchingCategory];
    }

    private Product[] CreateProductsMatchingPriceRange(decimal minPrice, decimal maxPrice)
    {
        var productMatchingMinPrice = _builder.ProductBuilder
            .WithId(103)
            .WithName()
            .WithDescription()
            .WithSalePrice(minPrice)
            .WithCategory()
            .Build();
        var productMatchingMaxPrice = _builder.ProductBuilder
            .WithId(104)
            .WithName()
            .WithDescription()
            .WithSalePrice(maxPrice)
            .WithCategory()
            .Build();

        return [productMatchingMinPrice, productMatchingMaxPrice];
    }

    private Product[] CreateProductsMatchingAllFilters(GetProductsRequest request)
    {
        var category = _builder.CategoryBuilder
            .WithId(request.CategoryId!.Value)
            .Build();

        var productMatchingAllFilters = _builder.ProductBuilder
            .WithId(105)
            .WithName(request.SearchTerm)
            .WithDescription(request.SearchTerm)
            .WithSalePrice(request.MinPrice!.Value)
            .WithCategory(category)
            .Build();

        return [productMatchingAllFilters];
    }
}
