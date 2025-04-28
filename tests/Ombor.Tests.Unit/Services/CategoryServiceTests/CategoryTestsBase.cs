using MockQueryable.Moq;
using Ombor.Application.Services;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Generators.Entities;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public abstract class CategoryTestsBase : ServiceTestsBase
{
    private protected readonly CategoryService _service;
    protected readonly Category[] _defaultCategories;

    protected CategoryTestsBase()
    {
        _defaultCategories = CategoryGenerator.Generate(5).ToArray();
        SetupCategories(_defaultCategories);

        _service = new CategoryService(_mockContext.Object, _mockValidator.Object);
    }

    public static TheoryData<GetCategoriesRequest> EmptyRequests => new()
    {
        { new GetCategoriesRequest(null) },
        { new GetCategoriesRequest(string.Empty) },
        { new GetCategoriesRequest(" ") },
        { new GetCategoriesRequest("    ") },
    };

    protected void SetupCategories(IEnumerable<Category> categories)
    {
        var categoriesList = categories.ToList();
        for (int i = 0; i < categoriesList.Count; i++)
        {
            categoriesList[i].Id = i + 1;
        }

        var mockDbSet = categoriesList.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Categories)
            .Returns(mockDbSet.Object);
    }
}
