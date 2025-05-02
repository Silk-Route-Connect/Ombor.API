using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class CreateCategoryTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldReturnCreated_WhenCategoryIsValid()
    {
        // Arrange
        var request = CategoryRequestFactory.GenerateValidCreateRequest();

        // Act
        var response = await _client.PostAsync<CreateCategoryResponse>(Routes.Category, request, HttpStatusCode.Created);

        // Assert
        await _responseValidator.Category.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = CategoryRequestFactory.GenerateInvalidCreateRequest();

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(Routes.Category, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Category.Name), response.Errors.Keys);
    }
}
