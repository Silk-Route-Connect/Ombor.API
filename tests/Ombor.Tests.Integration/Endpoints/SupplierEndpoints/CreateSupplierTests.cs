using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Supplier;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Endpoints.ProductEndpoints;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.SupplierEndpoints;

public class CreateSupplierTests(TestingWebApplicationFactory factory, ITestOutputHelper testOutput)
        : SupplierTestsBase(factory, testOutput)
{
    [Fact]
    public async Task PostAsync_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = SupplierRequestFactory.GenerateValidCreateRequest();

        // Act
        var response = await _client.PostAsync<CreateSupplierResponse>(Routes.Supplier, request, System.Net.HttpStatusCode.Created);

        // Assert
        await _responseValidator.Supplier.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        ///Arrange
        var request = SupplierRequestFactory.GenerateInvalidCreateRequest();

        ///Act
        var response = await _client.PostAsync<ValidationProblemDetails>(Routes.Supplier, request, System.Net.HttpStatusCode.BadRequest);

        ///Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Supplier.Name), response.Errors.Keys);
    }
}
