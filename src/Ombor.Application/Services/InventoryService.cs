using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class InventoryService(
    IApplicationDbContext context,
    IRequestValidator validator) : IInventoryService
{
    public async Task<PagedList<InventoryDto>> GetAsync(GetInventoriesRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var query = GetQuery(request);
        query = ApplySort(query, request.SortBy);

        var totalCount = await query.CountAsync();

        var inventories = await query
            .Include(x => x.InventoryItems)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var inventoriesDtos = inventories.Select(x => x.ToDto());

        return PagedList<InventoryDto>.ToPagedList(inventoriesDtos, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<InventoryDto> GetByIdAsync(GetInventoryByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
    }

    public async Task<CreateInventoryResponse> CreateAsync(CreateInventoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();
        context.Inventories.Add(entity);
        await context.SaveChangesAsync();

        return entity.ToCreateResponse();
    }

    public async Task<UpdateInventoryResponse> UpdateAsync(UpdateInventoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);
        entity.ApplyUpdate(request);

        await context.SaveChangesAsync();

        return entity.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeleteInventoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        context.Inventories.Remove(entity);
        await context.SaveChangesAsync();
    }

    private IQueryable<Inventory> GetQuery(GetInventoriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Inventories.AsNoTracking();
        var searchTerm = request.SearchTerm;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm) ||
                (x.Location != null && x.Location.Contains(searchTerm)));
        }

        return query;
    }

    private IQueryable<Inventory> ApplySort(IQueryable<Inventory> query, string? sortBy)
        => sortBy?.ToLower() switch
        {
            "location_asc" => query.OrderBy(x => x.Location),
            "location_desc" => query.OrderByDescending(x => x.Location),
            "name_desc" => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

    private async Task<Inventory> GetOrThrowAsync(int id) =>
       await context.Inventories.FirstOrDefaultAsync(x => x.Id == id)
       ?? throw new EntityNotFoundException<Inventory>(id);
}
