using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
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
        var product = await CreateProductAsync();
        var imagesToDelete = product.Images
            .Take(2)
            .Select(x => x.Id)
            .ToArray();
        var request = ProductRequestFactory.GenerateValidUpdateRequestWithAttachments(product.Id, DefaultCategoryId, imagesToDelete);
        var multipartForm = request.ToMultipartFormData();
        var url = GetUrl(product.Id);

        // Act
        _outputHelper.WriteLine($"Sending put request with body: {JsonConvert.SerializeObject(request)}");
        var response = await _client.PutAsync<UpdateProductResponse>(url, multipartForm);

        // Assert
        await _responseValidator.Product.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);
        var multipartForm = request.ToMultipartFormData();

        // Act
        _outputHelper.WriteLine($"Sending put request with body: {JsonConvert.SerializeObject(request)}");
        var response = await _client.PutAsync<ProblemDetails>(NotFoundUrl, multipartForm, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Product>(NonExistentEntityId);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var product = await CreateProductAsync();
        var request = ProductRequestFactory.GenerateInvalidUpdateRequest(product.Id);
        var multipartForm = request.ToMultipartFormData();
        var url = GetUrl(product.Id);

        // Act
        _outputHelper.WriteLine($"Sending put request with body: {JsonConvert.SerializeObject(request)}");
        var response = await _client.PutAsync<ValidationProblemDetails>(url, multipartForm, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Product.Name), response.Errors.Keys);
    }

    private Task<Product> CreateProductAsync()
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

        return CreateProductAsync(product, "product-1.jpg", "product-2.jpg");
    }
}
