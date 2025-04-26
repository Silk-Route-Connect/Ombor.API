using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class GetCategoryTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetAsync_ShouldReturnSuccess()
    {
        // Arrange
        var request = new GetCategoriesRequest(string.Empty);
        var url = GetUrl();

        // Act
        var response = await _client.GetAsync<CategoryDto[]>(url);

        // Assert
        await _responseValidator.Category.ValidateGetAsync(request, response);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnFilteredCategories_WhenSearchIsProvided()
    {
        // Arrange
        await CreateCategories(_searchTerm, 3);
        var request = new GetCategoriesRequest(_searchTerm);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<CategoryDto[]>(url);

        // Assert
        await _responseValidator.Category.ValidateGetAsync(request, response);
    }
}
