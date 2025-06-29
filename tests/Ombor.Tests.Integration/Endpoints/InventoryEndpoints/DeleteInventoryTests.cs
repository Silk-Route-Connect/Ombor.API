using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.InventoryEndpoints;

public class DeleteInventoryTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : InventoryTestsBase(factory, outputHelper)
{
    public async Task DeleteAsync_ShouldDeleteInventory_WhenInventoryExists()
    {
        // Arrange
        var inventoryToDelete = await CreateInventoryAsync();
        var url = GetUrl(inventoryToDelete.Id);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Inventory.ValidateDeleteAsync(inventoryToDelete.Id);
    }

    public async Task DeleteAsync_ShouldReturnNotFound_WhenInventoryDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<ProblemDetails>(NonExistentEntityId);
    }
}
