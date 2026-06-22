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

        return [.. warehouses.Select(x => x.ToDto())];
    }

    public async Task<WarehouseDto> GetByIdAsync(GetWarehouseByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
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

        return entity.ToDto();
    }

    public async Task<WarehouseDto> UpdateAsync(UpdateWarehouseRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);
        await EnsureNameIsUniqueAsync(request.Name, excludingId: entity.Id);

        entity.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return entity.ToDto();
    }

    public async Task<WarehouseDto> ArchiveAsync(int id)
    {
        var entity = await GetOrThrowAsync(id);
        entity.IsArchived = true;
        await context.SaveChangesAsync();

        return entity.ToDto();
    }

    public async Task<WarehouseDto> RestoreAsync(int id)
    {
        var entity = await GetOrThrowAsync(id);
        entity.IsArchived = false;
        await context.SaveChangesAsync();

        return entity.ToDto();
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

    private async Task<Warehouse> GetOrThrowAsync(int id) =>
       await context.Warehouses.FirstOrDefaultAsync(x => x.Id == id)
       ?? throw new EntityNotFoundException<Warehouse>(id);
}
