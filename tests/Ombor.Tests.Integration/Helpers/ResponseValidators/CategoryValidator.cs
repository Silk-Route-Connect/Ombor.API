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

        foreach (var expected in expectedCategories)
        {
            var actual = response.FirstOrDefault(c => c.Id == expected.Id);

            Assert.NotNull(actual);
            Assert.Equivalent(expected, actual);
        }
    }

    public async Task ValidateGetByIdAsync(GetCategoryByIdRequest request, CategoryDto response)
    {
        var expected = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id);

        Assert.NotNull(expected);
        Assert.Equivalent(expected, response);
    }

    public async Task ValidatePostAsync(CreateCategoryRequest request, CreateCategoryResponse response)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == response.Id);

        Assert.NotNull(category);
        Assert.Equal(request.Name, category.Name);
        Assert.Equal(request.Description, category.Description);
        Assert.Equal(response.Id, category.Id);
        Assert.Equal(response.Name, category.Name);
        Assert.Equal(response.Description, category.Description);
    }

    public async Task ValidatePutAsync(UpdateCategoryRequest request, UpdateCategoryResponse response)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id);

        Assert.NotNull(category);
        Assert.Equal(request.Name, category.Name);
        Assert.Equal(request.Description, category.Description);
        Assert.Equal(response.Id, category.Id);
        Assert.Equal(response.Name, category.Name);
        Assert.Equal(response.Description, category.Description);
    }

    public async Task ValidateDeleteAsync(DeleteCategoryRequest request)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id);

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
