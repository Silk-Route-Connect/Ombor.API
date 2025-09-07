using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.InventoryEndpoints;

public class UpdateInventoryTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : InventoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PutAsync_ShouldReturnUpdatedInventory_WhenRequestIsValid()
    {
        // Arrange
        var inventory = await CreateInventoryAsync();
        var request = InventoryRequestFactory.GenerateValidUpdateRequest(inventory.Id);
        var url = GetUrl(inventory.Id);

        // Act
        var response = await _client.PutAsync<UpdateInventoryResponse>(url, request);

        // Assert
        await _responseValidator.Inventory.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var inventory = await CreateInventoryAsync();
        var request = InventoryRequestFactory.GenerateInvalidUpdateRequest(inventory.Id);
        var url = GetUrl(inventory.Id);

        // Act
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Inventory.Name), response.Errors.Keys);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenInventoryDoesNotExist()
    {
        // Arrange
        var url = NotFoundUrl;
        var request = InventoryRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act
        var response = await _client.PutAsync<ProblemDetails>(url, request, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Inventory>(NonExistentEntityId);
    }
}
