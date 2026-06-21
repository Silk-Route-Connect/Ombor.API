using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

public class UpdateWarehouseTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : WarehouseTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PutAsync_ShouldReturnUpdatedWarehouse_WhenRequestIsValid()
    {
        // Arrange
        var warehouse = await CreateWarehouseAsync();
        var request = WarehouseRequestFactory.GenerateValidUpdateRequest(warehouse.Id);
        var url = GetUrl(warehouse.Id);

        // Act
        var response = await _client.PutAsync<WarehouseDto>(url, request);

        // Assert
        await _responseValidator.Warehouse.ValidatePutAsync(request, response);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Location, response.Location);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var warehouse = await CreateWarehouseAsync();
        var request = WarehouseRequestFactory.GenerateInvalidUpdateRequest(warehouse.Id);
        var url = GetUrl(warehouse.Id);

        // Act
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Warehouse.Name), response.Errors.Keys);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenWarehouseDoesNotExist()
    {
        // Arrange
        var url = NotFoundUrl;
        var request = WarehouseRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act
        var response = await _client.PutAsync<ProblemDetails>(url, request, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Warehouse>(NonExistentEntityId);
    }
}
