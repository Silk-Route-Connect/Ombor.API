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

        var query = context.Categories
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Name.Contains(request.SearchTerm) ||
                (x.Description != null && x.Description.Contains(request.SearchTerm)));
        }

        return query
            .Select(x => new CategoryDto(x.Id, x.Name, x.Description))
            .ToArrayAsync();
    }

    public async Task<CategoryDto> GetByIdAsync(GetCategoryByIdRequest request)
    {
        validator.ValidateAndThrow(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
    }

    public async Task<CreateCategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        validator.ValidateAndThrow(request);

        var entity = request.ToEntity();
        context.Categories.Add(entity);
        await context.SaveChangesAsync();

        return entity.ToCreateResponse();
    }

    public async Task<UpdateCategoryResponse> UpdateAsync(UpdateCategoryRequest request)
    {
        validator.ValidateAndThrow(request);

        var entity = await GetOrThrowAsync(request.Id);

        entity.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return entity.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeleteCategoryRequest request)
    {
        validator.ValidateAndThrow(request);

        var entity = await GetOrThrowAsync(request.Id);

        context.Categories.Remove(entity);
        await context.SaveChangesAsync();
    }

    private async Task<Category> GetOrThrowAsync(int id) =>
        await context.Categories.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Category>(id);
}
