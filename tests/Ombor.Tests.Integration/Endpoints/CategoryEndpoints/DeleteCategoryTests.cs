using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class DeleteCategoryTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenCategoryExists()
    {
        // Arrange
        var categoryToDelete = _builder.CategoryBuilder
            .WithName("Category To Delete")
            .Build();
        var categoryId = await CreateCategoryAsync(categoryToDelete);
        var url = GetUrl(categoryId);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Category.ValidateDeleteAsync(categoryId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Category>(NonExistentEntityId);
    }
}
