using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.InventoryEndpoints;

public class GetInventoryByIdTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : InventoryTestsBase(factory, outputHelper)
{

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenInventoryDoesNotExist()
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
    public async Task GetByIdAsync_ShouldReturnInventory_WhenInventoryExists()
    {
        // Arrange
        var inventory = await CreateInventoryAsync();
        var url = GetUrl(inventory.Id);

        // Act
        var response = await _client.GetAsync<InventoryDto>(url);

        // Assert
        await _responseValidator.Inventory.ValidateGetByIdAsync(inventory.Id, response);
    }
}
