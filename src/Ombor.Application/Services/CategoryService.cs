using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class CategoryService(IApplicationDbContext context, IRequestValidator validator) : ICategoryService
{
    public async Task<PagedList<CategoryDto>> GetAsync(GetCategoriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery(request);
        query = ApplySort(query, request.SortBy);

        var totalCount = await query.CountAsync();

        var categories = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var categoriesDto = categories.Select(x => x.ToDto());

        return PagedList<CategoryDto>.ToPagedList(categoriesDto, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<CategoryDto> GetByIdAsync(GetCategoryByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
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

        context.Categories.Remove(entity);
        await context.SaveChangesAsync();
    }

    private async Task<Category> GetOrThrowAsync(int id) =>
        await context.Categories.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Category>(id);

    private IQueryable<Category> GetQuery(GetCategoriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Categories.AsNoTracking();

        var searchTerm = request.SearchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(
                x => x.Name.Contains(searchTerm) ||
                (x.Description != null && x.Description.Contains(searchTerm)));
        }

        return query;
    }

    private IQueryable<Category> ApplySort(IQueryable<Category> query, string? sortBy)
        => sortBy?.ToLower() switch
        {
            "name_desc" => query.OrderByDescending(x => x.Name),
            "description_asc" => query.OrderBy(x => x.Description),
            "description_desc" => query.OrderByDescending(x => x.Description),
            _ => query.OrderBy(x => x.Name),
        };
}
