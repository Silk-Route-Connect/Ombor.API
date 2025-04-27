using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class CreateProductTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task CreateAsync_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var response = await _client.PostAsync<CreateProductResponse>(GetUrl(), request);

        // Assert
        await _responseValidator.Product.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = CreateInvalidRequest();

        // Act
        var response = await _client.PostAsync<ProblemDetails>(GetUrl(), request, HttpStatusCode.BadRequest);

        // Assert
        // TODO: Validate the error messages in the response
        Assert.NotNull(response);
    }

    private static CreateProductRequest CreateValidRequest() =>
        new(CategoryId: 1,
            Name: "Test Product1",
            SKU: $"Test SKU {Guid.NewGuid()}",
            Measurement: nameof(UnitOfMeasurement.Unit),
            Description: "Test Product Description",
            Barcode: "1234567890123",
            SalePrice: 11.99m,
            SupplyPrice: 9.99m,
            RetailPrice: 12.99m,
            QuantityInStock: 100,
            LowStockThreshold: 10,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

    private static CreateProductRequest CreateInvalidRequest() =>
        new(CategoryId: _nonExistentEntityId, // Invalid category ID
            Name: "", // Invalid name
            SKU: "", // Invalid SKU
            Measurement: nameof(UnitOfMeasurement.Unit),
            Description: "Test Product Description",
            Barcode: "1234567890123",
            SalePrice: 11.99m,
            SupplyPrice: 9.99m,
            RetailPrice: 12.99m,
            QuantityInStock: 100,
            LowStockThreshold: 10,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));
}
