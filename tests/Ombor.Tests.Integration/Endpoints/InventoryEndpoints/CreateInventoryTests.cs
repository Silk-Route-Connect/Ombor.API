using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.InventoryEndpoints;

public class CreateInventoryTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : InventoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldReturnCreated_WhenInventoryIsValid()
    {
        // Arrange
        var request = InventoryRequestFactory.GenerateValidCreateRequest();

        // Act
        var response = await _client.PostAsync<CreateInventoryResponse>(Routes.Inventory, request, HttpStatusCode.Created);

        // Assert
        await _responseValidator.Inventory.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenInventoryIsInvalid()
    {
        // Arrange
        var request = InventoryRequestFactory.GenerateInvalidCreateRequest();

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(Routes.Inventory, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Inventory.Name), response.Errors.Keys);
    }
}
