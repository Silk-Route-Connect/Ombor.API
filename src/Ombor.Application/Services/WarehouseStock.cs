using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

/// <summary>
/// The stock side of a warehouse behind <see cref="IWarehouseService"/>: its «Остатки» rows and its opening stock.
/// </summary>
internal sealed class WarehouseStock(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser,
    IOrganizationWriteLock writeLock)
{
    public async Task<WarehouseStockItemDto[]> GetRowsAsync(int warehouseId)
    {
        var warehouse = await GetWarehouseOrThrowAsync(warehouseId);

        var items = await context.WarehouseItems
            .Where(x => x.WarehouseId == warehouseId)
            .Include(x => x.Product)
            .ThenInclude(p => p.Category)
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .OrderBy(x => x.Product.Name)
            .ToArrayAsync();

        return [.. items.Select(x => x.ToStockItemDto(warehouse.IsArchived))];
    }

    public async Task AddOpeningStockAsync(AddOpeningStockRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // «Not stocked yet» is checked under the organization's write lock, so two parallel openings (or an opening
        // racing a supply) can't both stock the same product.
        await using var write = await writeLock.BeginOrgWriteAsync();

        _ = await GetWarehouseOrThrowAsync(request.WarehouseId);

        await OwnedReferences.Check()
            .Require(context.Products, request.Items.Select((x, i) => (x.ProductId, $"Items[{i}].ProductId")))
            .ThrowIfMissingAsync();

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
        await write.CommitAsync();
    }

    private async Task<Warehouse> GetWarehouseOrThrowAsync(int id) =>
        await context.Warehouses
            .IgnoreAutoIncludes()
            .FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Warehouse>(id);
}
