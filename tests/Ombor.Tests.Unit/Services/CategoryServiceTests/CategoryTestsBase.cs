using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public abstract class CategoryTestsBase : ServiceTestsBase
{
    protected readonly int CategoryId = 1_000; // ID to be used in GetById, Update, Delete Tests
    protected readonly Category[] _defaultCategories;
    private protected readonly CategoryService _service;

    protected CategoryTestsBase()
    {
        _defaultCategories = GenerateRandomCategories();
        SetupCategories(_defaultCategories);

        _service = new CategoryService(_mockContext.Object, _mockValidator.Object);
    }

    protected Category[] GenerateRandomCategories(int count = 5)
        => Enumerable.Range(1, count)
        .Select(i => CreateCategory(i))
        .ToArray();

    protected Category CreateCategory(int? id = null) => _builder.CategoryBuilder
        .WithId(id ?? CategoryId)
        .BuildAndPopulate();
}
