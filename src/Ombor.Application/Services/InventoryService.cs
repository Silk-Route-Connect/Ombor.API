using FluentValidation;
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
    IRequestValidator validator) : IInventoryService
{
    public async Task<InventoryDto[]> GetAsync(GetInventoriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Inventories.AsQueryable();

        var searchTerm = request.SearchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm) ||
                (x.Location != null && x.Location.Contains(searchTerm)));
        }

        var inventories = await query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToArrayAsync();

        return [.. inventories.Select(x => x.ToDto())];
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

    public async Task<InventoryDto> AddOpeningStockAsync(AddOpeningStockRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var inventory = await GetOrThrowAsync(request.InventoryId);

        var productIds = request.Items.Select(x => x.ProductId).ToArray();

        var alreadyStocked = await context.InventoryItems
            .Where(x => x.InventoryId == request.InventoryId && productIds.Contains(x.ProductId))
            .Select(x => x.ProductId)
            .ToArrayAsync();

        if (alreadyStocked.Length > 0)
        {
            throw new ValidationException(
                $"Products [{string.Join(", ", alreadyStocked)}] are already stocked in this warehouse. " +
                "Use a Supply transaction to add more stock.");
        }

        foreach (var item in request.Items)
        {
            inventory.InventoryItems.Add(new InventoryItem
            {
                InventoryId = request.InventoryId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                AverageCost = item.UnitCost,
                Inventory = null!,
                Product = null!,
            });
        }

        await context.SaveChangesAsync();

        return inventory.ToDto();
    }

    private async Task<Inventory> GetOrThrowAsync(int id) =>
       await context.Inventories.FirstOrDefaultAsync(x => x.Id == id)
       ?? throw new EntityNotFoundException<Inventory>(id);
}
