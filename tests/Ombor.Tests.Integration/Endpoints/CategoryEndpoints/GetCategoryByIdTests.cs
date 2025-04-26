using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class GetCategoryByIdTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetAsync_ShouldReturnOk_WhenCategoryExists()
    {
        // Arrange
        var categoryId = await CreateCategoryAsync();
        var request = new GetCategoryByIdRequest(categoryId);
        var url = GetUrl(categoryId);

        // Act
        var response = await _client.GetAsync<CategoryDto>(url);

        // Assert
        await _responseValidator.Category.ValidateGetByIdAsync(request, response);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = 999999; // Non-existent category IDs

        // Act
        var response = await _client.GetAsync<ProblemDetails>($"{Routes.Category}/{categoryId}", HttpStatusCode.NotFound);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(NotFoundTitle, response.Title);
        Assert.Equal(GetNotFoundErrorMessage(categoryId, nameof(Category)), response.Detail);
    }

    private async Task<int> CreateCategoryAsync()
    {
        var category = new Category
        {
            Name = "Electronics",
            Description = "Devices and gadgets"
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return category.Id;
    }
}
