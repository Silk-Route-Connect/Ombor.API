using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public abstract class CategoryTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestsBase(factory, outputHelper)
{
    protected readonly int _nonExistentCategoryId = 99999;
    protected readonly string _searchTerm = "Electornics";

    protected string NotFoundUrl => GetUrl(_nonExistentCategoryId);

    protected async Task<int> CreateCategoryAsync()
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

    protected async Task CreateCategories(string name, int count = 3)
    {
        var categories = new List<Category>();

        for (var i = 0; i < count; i++)
        {
            var category = new Category
            {
                Name = $"{name} {i}",
                Description = $"Description {i}"
            };
            categories.Add(category);
        }

        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();
    }

    protected override string GetUrl()
        => Routes.Category;

    protected override string GetUrl(int id)
        => $"{Routes.Category}/{id}";
}
