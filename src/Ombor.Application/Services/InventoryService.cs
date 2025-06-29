using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class InventoryService(
    IApplicationDbContext context,
    IInventoryMapping mapping,
    IRequestValidator validator) : IInventoryService
{
    public Task<InventoryDto[]> GetAsync(GetInventoriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Inventories.AsQueryable();

        var searchTerm = request.SearchTerm;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(
                x => x.Name.Contains(searchTerm) ||
                (x.Location != null && x.Location.Contains(searchTerm)));
        }

        return query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new InventoryDto(
                x.Id,
                x.Name,
                x.Location,
                x.IsActive))
            .ToArrayAsync();
    }

    public async Task<InventoryDto> GetByIdAsync(GetInventoryByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return mapping.ToDto(entity);
    }

    public async Task<CreateInventoryResponse> CreateAsync(CreateInventoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = mapping.ToEntity(request);
        context.Inventories.Add(entity);
        await context.SaveChangesAsync();

        return mapping.ToCreateResponse(entity);
    }

    public async Task<UpdateInventoryResponse> UpdateAsync(UpdateInventoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);
        mapping.ApplyUpdate(entity, request);

        await context.SaveChangesAsync();

        return mapping.ToUpdateResponse(entity);
    }

    public async Task DeleteAsync(DeleteInventoryRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        context.Inventories.Remove(entity);
        await context.SaveChangesAsync();
    }

    private async Task<Inventory> GetOrThrowAsync(int id) =>
       await context.Inventories.FirstOrDefaultAsync(x => x.Id == id)
       ?? throw new EntityNotFoundException<Inventory>(id);
}
