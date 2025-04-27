using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class UpdateCategoryTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PutAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var categoryId = await CreateCategoryAsync();
        var request = CreateValidRequest(categoryId);
        var url = GetUrl(categoryId);

        // Act
        var response = await _client.PutAsync<UpdateCategoryResponse>(url, request);

        // Assert
        await _responseValidator.Category.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = CreateValidRequest(_nonExistentEntityId);

        // Act
        var response = await _client.PutAsync<ProblemDetails>(NotFoundUrl, request, HttpStatusCode.NotFound);

        // Assert
        response.NotFound<Category>(_nonExistentEntityId);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var categoryId = await CreateCategoryAsync();
        var request = CreateInvalidRequest(categoryId);
        var url = GetUrl(categoryId);

        // Act
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        // TODO: Validate the error messages in the response
        Assert.NotNull(response);
    }

    private static UpdateCategoryRequest CreateValidRequest(int id) =>
        new(id, "Updated Category", "Updated Description");

    private static UpdateCategoryRequest CreateInvalidRequest(int id) =>
        new(id, string.Empty, string.Empty); // Invalid name
}
