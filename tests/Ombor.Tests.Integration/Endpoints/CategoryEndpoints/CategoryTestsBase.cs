using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public abstract class CategoryTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestsBase(factory, outputHelper)
{
    protected readonly string _searchTerm = "Electornics";

    protected override string GetUrl()
        => Routes.Category;

    protected override string GetUrl(int id)
        => $"{Routes.Category}/{id}";

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

    protected async Task<int> CreateCategoryAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return category.Id;
    }
}
