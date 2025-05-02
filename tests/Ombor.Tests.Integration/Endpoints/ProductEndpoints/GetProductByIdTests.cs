using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class GetProductByIdTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk_WhenProductExists()
    {
        // Arrange
        var category = _builder.CategoryBuilder
            .WithName("Category for fetching product")
            .Build();
        var product = _builder.ProductBuilder
            .WithName("Product To Fetch")
            .WithCategory(category)
            .Build();
        var productId = await CreateProductAsync(product);
        var url = GetUrl(productId);

        // Act
        var response = await _client.GetAsync<ProductDto>(url);

        // Assert
        await _responseValidator.Product.ValidateGetByIdAsync(productId, response);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Product>(NonExistentEntityId);
    }
}
