using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Product;
using Ombor.Tests.Common.Factories;
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
        var request = ProductRequestFactory.GenerateValidCreateRequest(categoryId: 1);

        // Act
        var response = await _client.PostAsync<CreateProductResponse>(GetUrl(), request);

        // Assert
        await _responseValidator.Product.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateInvalidCreateRequest(categoryId: 1);

        // Act
        var response = await _client.PostAsync<ProblemDetails>(GetUrl(), request, HttpStatusCode.BadRequest);

        // Assert
        // TODO: Validate the error messages in the response
        Assert.NotNull(response);
    }
}
