using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
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
        var productId = await CreateProductAsync(categoryId: 1);
        var request = CreateValidRequest(productId);
        var url = GetUrl(productId);

        // Act
        var response = await _client.PutAsync<UpdateProductResponse>(url, request);

        // Assert
        await _responseValidator.Product.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var request = CreateValidRequest(_nonExistentEntityId);

        // Act
        var response = await _client.PutAsync<ProblemDetails>(NotFoundUrl, request, HttpStatusCode.NotFound);

        // Assert
        response.NotFound<Product>(_nonExistentEntityId);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var productId = await CreateProductAsync(categoryId: 1);
        var request = CreateInvalidRequest(productId);
        var url = GetUrl(productId);

        // Act
        var response = await _client.PutAsync<ProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        // TODO: Validate the error messages in the response
        Assert.NotNull(response);
    }

    private static UpdateProductRequest CreateValidRequest(int productId) =>
        new(Id: productId,
            CategoryId: 1,
            Name: "Updated Product",
            SKU: "Updated SKU",
            Measurement: nameof(UnitOfMeasurement.Unit),
            Description: "Updated Product Description",
            Barcode: "1234567890123",
            SalePrice: 15.99m,
            SupplyPrice: 12.99m,
            RetailPrice: 16.99m,
            QuantityInStock: 150,
            LowStockThreshold: 20,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20))
        );

    private static UpdateProductRequest CreateInvalidRequest(int productId) =>
        new(Id: productId,
            CategoryId: _nonExistentEntityId, // Invalid category ID
            Name: "", // Invalid name
            SKU: "", // Invalid SKU
            Measurement: nameof(UnitOfMeasurement.Unit),
            Description: "Updated Product Description",
            Barcode: "1234567890123",
            SalePrice: 15.99m,
            SupplyPrice: 12.99m,
            RetailPrice: 16.99m,
            QuantityInStock: 150,
            LowStockThreshold: 20,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20))
        );
}
