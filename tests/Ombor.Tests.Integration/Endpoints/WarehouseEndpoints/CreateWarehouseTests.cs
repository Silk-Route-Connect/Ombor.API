using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

public class CreateWarehouseTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : WarehouseTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldReturnCreated_WhenWarehouseIsValid()
    {
        // Arrange
        var request = WarehouseRequestFactory.GenerateValidCreateRequest();

        // Act
        var response = await _client.PostAsync<WarehouseDto>(Routes.Warehouse, request, HttpStatusCode.Created);

        // Assert — a new warehouse is created empty (zero totals) and not archived.
        await _responseValidator.Warehouse.ValidatePostAsync(request, response);
        Assert.Equal(0, response.ProductCount);
        Assert.Equal(0, response.TotalUnits);
        Assert.Equal(0m, response.StockValue);
        Assert.False(response.IsArchived);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenWarehouseIsInvalid()
    {
        // Arrange
        var request = WarehouseRequestFactory.GenerateInvalidCreateRequest();

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(Routes.Warehouse, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Warehouse.Name), response.Errors.Keys);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenNameIsDuplicate()
    {
        // Arrange — a warehouse with the same name already exists.
        var existing = await CreateWarehouseAsync();
        var request = new CreateWarehouseRequest(existing.Name, "Somewhere else");

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(Routes.Warehouse, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Warehouse.Name), response.Errors.Keys);
    }
}
