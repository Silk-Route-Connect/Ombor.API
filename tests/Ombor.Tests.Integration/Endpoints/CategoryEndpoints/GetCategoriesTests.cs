using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class GetCategoriesTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    private const string _matchingSearchTerm = "Test Category";

    [Fact]
    public async Task GetAsync_ShouldReturnFilteredCategories_WhenSearchIsProvided()
    {
        // Arrange
        var request = new GetCategoriesRequest(_searchTerm);
        await CreateCategories(request);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<CategoryDto[]>(url);

        // Assert
        await _responseValidator.Category.ValidateGetAsync(request, response);
    }

    private async Task CreateCategories(GetCategoriesRequest request)
    {
        var searchTerm = request.SearchTerm ?? _matchingSearchTerm;

        var categories = new List<Category>
        {
            // Matching search term by name
            new()
            {
                Name = searchTerm,
                Description = "Test Category Description 1"
            },
            // Matching search term by description
            new()
            {
                Name = "Test Category 2",
                Description = searchTerm,
            }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();
    }
}
