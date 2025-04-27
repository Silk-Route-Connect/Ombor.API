using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class GetCategoryByIdTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk_WhenCategoryExists()
    {
        // Arrange
        var categoryId = await CreateCategoryAsync();
        var url = GetUrl(categoryId);

        // Act
        var response = await _client.GetAsync<CategoryDto>(url);

        // Assert
        await _responseValidator.Category.ValidateGetByIdAsync(categoryId, response);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.NotFound<Category>(_nonExistentEntityId);
    }
}
