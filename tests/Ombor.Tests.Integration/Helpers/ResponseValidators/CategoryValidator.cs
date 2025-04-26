using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class CategoryValidator(IApplicationDbContext context)
{
    public async Task ValidateGetAsync(GetCategoriesRequest request, CategoryDto[] response)
    {
        var expectedCategories = await GetCategoriesAsync(request);

        Assert.Equal(expectedCategories.Length, response.Length);
        Assert.All(expectedCategories, expected =>
        {
            var actual = response.FirstOrDefault(c => c.Id == expected.Id);

            Assert.NotNull(actual);
            AssertEquivalent(expected, actual);
        });
    }

    public async Task ValidateGetByIdAsync(GetCategoryByIdRequest request, CategoryDto response)
    {
        var expected = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id);

        Assert.NotNull(expected);
        AssertEquivalent(expected, response);
    }

    public async Task ValidatePostAsync(CreateCategoryRequest request, CreateCategoryResponse response)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == response.Id);

        Assert.NotNull(category);
        AssertEquivalent(category, response);
        AssertEquivalent(request, category);
    }

    public async Task ValidatePutAsync(UpdateCategoryRequest request, UpdateCategoryResponse response)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id);

        Assert.NotNull(category);
        AssertEquivalent(category, response);
        AssertEquivalent(request, category);
    }

    public async Task ValidateDeleteAsync(int categoryId)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        Assert.Null(category);
    }

    private async Task<Category[]> GetCategoriesAsync(GetCategoriesRequest request)
    {
        var query = context.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(c => c.Name.Contains(request.SearchTerm) ||
                (c.Description != null && c.Description.Contains(request.SearchTerm)));
        }

        return await query
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToArrayAsync();
    }

    private static void AssertEquivalent(Category expected, CategoryDto actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    private static void AssertEquivalent(Category expected, CreateCategoryResponse actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    private static void AssertEquivalent(Category expected, UpdateCategoryResponse actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    private static void AssertEquivalent(CreateCategoryRequest expected, Category actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }

    private static void AssertEquivalent(UpdateCategoryRequest expected, Category actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
    }
}
