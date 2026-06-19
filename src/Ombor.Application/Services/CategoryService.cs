using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class CategoryService(IApplicationDbContext context, IRequestValidator validator) : ICategoryService
{
    public Task<CategoryDto[]> GetAsync(GetCategoriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Name.Contains(request.SearchTerm) ||
                (x.Description != null && x.Description.Contains(request.SearchTerm)));
        }

        return query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Description, x.Products.Count))
            .ToArrayAsync();
    }

    public async Task<CategoryDto> GetByIdAsync(GetCategoryByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var dto = await context.Categories
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Description, x.Products.Count))
            .FirstOrDefaultAsync();

        return dto ?? throw new EntityNotFoundException<Category>(request.Id);
    }

    public async Task<CreateCategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();
        context.Categories.Add(entity);
        await context.SaveChangesAsync();

        return entity.ToCreateResponse();
    }

    public async Task<UpdateCategoryResponse> UpdateAsync(UpdateCategoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        entity.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return entity.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeleteCategoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        // Reference gate (rule 32 / contract §3): a category referenced by products
        // is not deletable. Archive is not offered for categories, so this is a hard 409.
        var productCount = await context.Products.CountAsync(p => p.CategoryId == request.Id);
        if (productCount > 0)
        {
            throw new ConflictException($"Категория содержит {productCount} товаров.");
        }

        context.Categories.Remove(entity);
        await context.SaveChangesAsync();
    }

    private async Task<Category> GetOrThrowAsync(int id) =>
        await context.Categories.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Category>(id);
}
