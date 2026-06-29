using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class WarehouseService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser) : IWarehouseService
{
    // A warehouse is "referenced" when any record links to it: stock (any item row, even zero-quantity),
    // an opening/adjustment movement, a transaction, a transfer on either side, or an order. Such a
    // warehouse is archive-only — hard-deleting it would orphan that history (rule 32). Archiving never
    // clears this: residual stock still counts (rule 31). This single predicate is the source of truth
    // for both the served IsDeletable flag and the delete guard, so the two can never diverge.
    private Expression<Func<Warehouse, bool>> IsReferenced => warehouse =>
        warehouse.WarehouseItems.Any() ||
        context.OpeningStocks.Any(x => x.WarehouseId == warehouse.Id) ||
        context.StockAdjustments.Any(x => x.WarehouseId == warehouse.Id) ||
        context.Transactions.Any(x => x.WarehouseId == warehouse.Id) ||
        context.Transfers.Any(x => x.FromWarehouseId == warehouse.Id || x.ToWarehouseId == warehouse.Id) ||
        context.Orders.Any(x => x.WarehouseId == warehouse.Id);

    public async Task<WarehouseDto[]> GetAsync(GetWarehousesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Warehouses.AsQueryable();

        var searchTerm = request.SearchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm) ||
                (x.Location != null && x.Location.Contains(searchTerm)));
        }

        // Archived warehouses are intentionally included (rule 31).
        var warehouses = await query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToArrayAsync();

        var referencedIds = await GetReferencedIdsAsync([.. warehouses.Select(x => x.Id)]);

        return [.. warehouses.Select(x => x.ToDto(isDeletable: !referencedIds.Contains(x.Id)))];
    }

    public async Task<WarehouseDto> GetByIdAsync(GetWarehouseByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto(isDeletable: !await IsReferencedAsync(entity.Id));
    }

    public async Task<WarehouseStockItemDto[]> GetStockAsync(GetWarehouseByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // 404 if the warehouse doesn't exist.
        _ = await GetOrThrowAsync(request.Id);

        var items = await context.WarehouseItems
            .Where(x => x.WarehouseId == request.Id)
            .Include(x => x.Product)
            .ThenInclude(p => p.Category)
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .OrderBy(x => x.Product.Name)
            .ToArrayAsync();

        return [.. items.Select(x => x.ToStockItemDto())];
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request)
    {
        await validator.ValidateAndThrowAsync(request);
        await EnsureNameIsUniqueAsync(request.Name, excludingId: null);

        var entity = request.ToEntity();
        context.Warehouses.Add(entity);
        await context.SaveChangesAsync();

        // A freshly created warehouse has no references yet, so it is always deletable.
        return entity.ToDto(isDeletable: true);
    }

    public async Task<WarehouseDto> UpdateAsync(UpdateWarehouseRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);
        await EnsureNameIsUniqueAsync(request.Name, excludingId: entity.Id);

        entity.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return entity.ToDto(isDeletable: !await IsReferencedAsync(entity.Id));
    }

    public async Task<WarehouseDto> ArchiveAsync(int id)
    {
        var entity = await GetOrThrowAsync(id);
        entity.IsArchived = true;
        await context.SaveChangesAsync();

        return entity.ToDto(isDeletable: !await IsReferencedAsync(entity.Id));
    }

    public async Task<WarehouseDto> RestoreAsync(int id)
    {
        var entity = await GetOrThrowAsync(id);
        entity.IsArchived = false;
        await context.SaveChangesAsync();

        return entity.ToDto(isDeletable: !await IsReferencedAsync(entity.Id));
    }

    public async Task DeleteAsync(DeleteWarehouseRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        // Referenced warehouses can't be hard-deleted (rule 32: 409, the UI steers to archive). Blocking
        // here also prevents the WarehouseItem cascade from silently wiping stock rows on delete.
        if (await IsReferencedAsync(entity.Id))
        {
            throw new ConflictException(
                "Warehouse cannot be deleted because other records reference it. Archive it instead.");
        }

        context.Warehouses.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<WarehouseDto> AddOpeningStockAsync(AddOpeningStockRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // 404 if the warehouse doesn't exist.
        _ = await GetOrThrowAsync(request.WarehouseId);

        var productIds = request.Items.Select(x => x.ProductId).ToArray();

        var alreadyStocked = await context.WarehouseItems
            .Where(x => x.WarehouseId == request.WarehouseId && productIds.Contains(x.ProductId))
            .Select(x => x.ProductId)
            .ToArrayAsync();

        if (alreadyStocked.Length > 0)
        {
            throw new ValidationException(
                $"Products [{string.Join(", ", alreadyStocked)}] are already stocked in this warehouse. " +
                "Use a Supply transaction to add more stock.");
        }

        // Record the opening as an immutable event per product (gives the movement ledger an "Opening" row).
        var now = DateTimeOffset.UtcNow;
        var createdById = currentUser.UserId;
        foreach (var item in request.Items)
        {
            context.OpeningStocks.Add(new OpeningStock
            {
                DateUtc = now,
                WarehouseId = request.WarehouseId,
                Warehouse = null!,
                ProductId = item.ProductId,
                Product = null!,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                Note = request.Note,
                CreatedById = createdById,
            });
        }

        // Set the stock through the shared path — a fresh item weighted-averages to the opening cost.
        await context.MoveStockAsync(
            request.WarehouseId,
            StockMovement.StockInWeightedAverage,
            request.Items.Select(i => (i.ProductId, i.Quantity, i.UnitCost)));

        await context.SaveChangesAsync();

        return await GetByIdAsync(new GetWarehouseByIdRequest(request.WarehouseId));
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? excludingId)
    {
        var exists = await context.Warehouses
            .AnyAsync(w => w.Name == name && (excludingId == null || w.Id != excludingId));

        if (exists)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CreateWarehouseRequest.Name),
                    $"A warehouse named '{name}' already exists."),
            ]);
        }
    }

    private Task<bool> IsReferencedAsync(int id) =>
        context.Warehouses
            .Where(x => x.Id == id)
            .Where(IsReferenced)
            .AnyAsync();

    private async Task<HashSet<int>> GetReferencedIdsAsync(int[] ids)
    {
        if (ids.Length == 0)
        {
            return [];
        }

        var referenced = await context.Warehouses
            .Where(x => ids.Contains(x.Id))
            .Where(IsReferenced)
            .Select(x => x.Id)
            .ToArrayAsync();

        return [.. referenced];
    }

    private async Task<Warehouse> GetOrThrowAsync(int id) =>
       await context.Warehouses.FirstOrDefaultAsync(x => x.Id == id)
       ?? throw new EntityNotFoundException<Warehouse>(id);
}
