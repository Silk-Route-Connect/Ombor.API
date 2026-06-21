using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

public class GetWarehouseByIdTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : WarehouseTestsBase(factory, outputHelper)
{

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenWarehouseDoesNotExist()
    {
        // Arrange
        var url = NotFoundUrl;

        // Act
        var response = await _client.GetAsync<ProblemDetails>(url, HttpStatusCode.NotFound);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(NotFoundTitle, response.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnWarehouse_WhenWarehouseExists()
    {
        // Arrange
        var warehouse = await CreateWarehouseAsync();
        var url = GetUrl(warehouse.Id);

        // Act
        var response = await _client.GetAsync<WarehouseDto>(url);

        // Assert
        await _responseValidator.Warehouse.ValidateGetByIdAsync(warehouse.Id, response);
    }
}
