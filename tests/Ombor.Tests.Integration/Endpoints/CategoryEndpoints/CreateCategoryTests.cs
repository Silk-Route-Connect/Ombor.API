using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class CreateCategoryTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task CreateAsync_ShouldReturnCreated_WhenCategoryIsValid()
    {
        // Arrange
        var request = GetValidRequest();

        // Act
        var response = await _client.PostAsync<CreateCategoryResponse>(Routes.Category, request, HttpStatusCode.Created);

        // Assert
        await _responseValidator.Category.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = GetInvalidRequest();

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(Routes.Category, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
    }

    private static CreateCategoryRequest GetValidRequest() =>
        new("Electronics", "Devices and gadgets");

    private static CreateCategoryRequest GetInvalidRequest() =>
        new(string.Empty, string.Empty); // Invalid name
}
