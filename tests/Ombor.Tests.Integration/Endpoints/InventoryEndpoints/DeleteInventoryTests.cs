using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.InventoryEndpoints;

public class DeleteInventoryTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : InventoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldDeleteInventory_WhenInventoryExists()
    {
        // Arrange
        var inventoryToDelete = await CreateInventoryAsync();
        var url = GetUrl(inventoryToDelete.Id);

        // Act
        await _client.DeleteAsync(url, HttpStatusCode.NoContent);

        // Assert
        await _responseValidator.Inventory.ValidateDeleteAsync(inventoryToDelete.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenInventoryDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Inventory>(NonExistentEntityId);
    }
}
