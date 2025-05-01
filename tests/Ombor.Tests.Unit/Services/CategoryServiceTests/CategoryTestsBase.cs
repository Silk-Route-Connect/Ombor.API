using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public abstract class CategoryTestsBase : ServiceTestsBase
{
    protected const int CategoryId = 1_000;
    private protected readonly CategoryService _service;
    protected readonly Category[] _defaultCategories;

    protected CategoryTestsBase()
    {
        _defaultCategories = GenerateRandomCategories();
        SetupCategories(_defaultCategories);

        _service = new CategoryService(_mockContext.Object, _mockValidator.Object);
    }

    protected Mock<DbSet<Category>> SetupCategories(IEnumerable<Category> categories)
    {
        var shuffledCategories = categories.ToArray();
        Random.Shared.Shuffle(shuffledCategories);

        var mockDbSet = shuffledCategories.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.Categories)
            .Returns(mockDbSet.Object);

        return mockDbSet;
    }

    protected Category[] GenerateRandomCategories(int count = 5)
        => Enumerable.Range(1, count)
        .Select(i => _builder.CategoryBuilder
            .WithId(i)
            .BuildAndPopulate())
        .ToArray();
}
