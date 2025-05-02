using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class DeleteProductTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenProductExists()
    {
        // Arrange
        var productId = await CreateProductAsync(DefaultCategoryId);
        var url = GetUrl(productId);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Product.ValidateDeleteAsync(productId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Product>(NonExistentEntityId);
    }
}
