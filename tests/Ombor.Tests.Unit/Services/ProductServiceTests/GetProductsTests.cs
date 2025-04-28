using AutoFixture;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public sealed class GetProductsTests : ProductTestsBase
{
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
        var request = _fixture.Create<GetProductsRequest>();
        SetupProducts([]);

        // Act
        var result = await _service.GetAsync(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnAll_WhenNoQueryParameterProvided()
    {
        // Arrange
        var request = new GetProductsRequest(null, null, null, null);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(_defaultProducts.Length, response.Length);
    }

    [Theory]
    [MemberData(nameof(GetRequests))]
    public async Task GetAsync_ShouldReturnMatchingProducts_WhenSearchTermIsProvided(GetProductsRequest request)
    {
        // Arrange
        var matchingProducts = CreateMatchingProducts(request);
        var randomProducts = CreateRandomProducts();
        Product[] allProducts = [.. randomProducts, .. matchingProducts];
        Random.Shared.Shuffle(allProducts);
        var expectedProducts = request.IsEmpty()
            ? allProducts
            : matchingProducts;

        SetupProducts(allProducts);

        // Act
        var result = await _service.GetAsync(request);

        // Assert
        Assert.Equal(expectedProducts.Length, result.Length);
        Assert.All(result, actual =>
        {
            var expected = expectedProducts.SingleOrDefault(x => x.Id == actual.Id);

            Assert.NotNull(expected);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Description, actual.Description);
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
            .Build();
        var matchingDescription = _builder.ProductBuilder
            .WithId(101)
            .WithName()
            .WithDescription(searchTerm)
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
            .Build();
        var productMatchingMaxPrice = _builder.ProductBuilder
            .WithId(104)
            .WithName()
            .WithDescription()
            .WithSalePrice(maxPrice)
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
