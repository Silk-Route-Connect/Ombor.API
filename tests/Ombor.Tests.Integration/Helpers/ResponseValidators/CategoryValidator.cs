using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Helpers;

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

            CategoryAssertionHelper.AssertEquivalent(expected, actual);
        });
    }

    public async Task ValidateGetByIdAsync(int categoryId, CategoryDto response)
    {
        var expected = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        CategoryAssertionHelper.AssertEquivalent(expected, response);
    }

    public async Task ValidatePostAsync(CreateCategoryRequest request, CreateCategoryResponse response)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == response.Id);

        CategoryAssertionHelper.AssertEquivalent(request, category);
        CategoryAssertionHelper.AssertEquivalent(request, response);
        CategoryAssertionHelper.AssertEquivalent(category, response);
    }

    public async Task ValidatePutAsync(UpdateCategoryRequest request, UpdateCategoryResponse response)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id);

        CategoryAssertionHelper.AssertEquivalent(request, category);
        CategoryAssertionHelper.AssertEquivalent(request, response);
        CategoryAssertionHelper.AssertEquivalent(category, response);
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
}
