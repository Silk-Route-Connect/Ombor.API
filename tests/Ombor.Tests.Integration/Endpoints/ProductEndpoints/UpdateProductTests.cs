using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class UpdateProductTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PutAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var productId = await CreateProductAsync();
        var request = ProductRequestFactory.GenerateValidUpdateRequest(productId, DefaultCategoryId);
        var url = GetUrl(productId);

        // Act
        _outputHelper.WriteLine($"Sending put request with body: {JsonConvert.SerializeObject(request)}");
        var response = await _client.PutAsync<UpdateProductResponse>(url, request);

        // Assert
        await _responseValidator.Product.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act
        _outputHelper.WriteLine($"Sending put request with body: {JsonConvert.SerializeObject(request)}");
        var response = await _client.PutAsync<ProblemDetails>(NotFoundUrl, request, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Product>(NonExistentEntityId);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var productId = await CreateProductAsync();
        var request = ProductRequestFactory.GenerateInvalidUpdateRequest(productId);
        var url = GetUrl(productId);

        // Act
        _outputHelper.WriteLine($"Sending put request with body: {JsonConvert.SerializeObject(request)}");
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Product.Name), response.Errors.Keys);
    }

    private Task<int> CreateProductAsync()
    {
        var category = _builder.CategoryBuilder
            .WithName("Category for updating product")
            .Build();
        var product = _builder.ProductBuilder
            .WithName("Product To Be Updated")
            .WithSKU()
            .WithDescription()
            .WithBarcode()
            .WithCategory(category)
            .Build();

        return CreateProductAsync(product);
    }
}
