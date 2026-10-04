using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.StockAdjustment;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using DomainDirection = Ombor.Domain.Enums.StockAdjustmentDirection;

namespace Ombor.Application.Services;

internal sealed class StockAdjustmentService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser,
    StockAdjustmentBalances balances,
    IOrganizationWriteLock writeLock) : IStockAdjustmentService
{
    public async Task<StockAdjustmentDto[]> GetAsync(GetStockAdjustmentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = WithDetails();

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == request.WarehouseId);
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == request.ProductId);
        }

        var adjustments = await query
            .OrderByDescending(x => x.DateUtc)
            .ThenByDescending(x => x.Id)
            .ToArrayAsync();

        var balanceAfter = await balances.ComputeAsync([.. adjustments.Select(a => new AdjustmentKey(a.Id, a.WarehouseId, a.ProductId))]);

        return [.. adjustments.Select(x => x.ToDto(balanceAfter.GetValueOrDefault(x.Id)))];
    }

    public async Task<StockAdjustmentDto> GetByIdAsync(GetStockAdjustmentByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var adjustment = await WithDetails().FirstOrDefaultAsync(x => x.Id == request.Id)
            ?? throw new EntityNotFoundException<StockAdjustment>(request.Id);

        var balanceAfter = await balances.ComputeAsync([new AdjustmentKey(adjustment.Id, adjustment.WarehouseId, adjustment.ProductId)]);

        return adjustment.ToDto(balanceAfter.GetValueOrDefault(adjustment.Id));
    }

    public async Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // Stock on hand is read under the organization's write lock, so parallel decreases cannot both pass rule 20.
        await using var write = await writeLock.BeginOrgWriteAsync();

        await OwnedReferences.Check()
            .Require(context.Warehouses, request.WarehouseId, nameof(request.WarehouseId))
            .Require(context.Products, request.ProductId, nameof(request.ProductId))
            .ThrowIfMissingAsync();

        var direction = request.Direction.ToDomainDirection();

        // Snapshot the WAC before moving stock: a decrease records the loss at the current carrying cost, an
        // increase restores units at that same cost (rule 18), so both serve a value.
        var unitCost = await context.WarehouseItems
            .Where(i => i.WarehouseId == request.WarehouseId && i.ProductId == request.ProductId)
            .Select(i => i.AverageCost)
            .FirstOrDefaultAsync();

        var movement = direction == DomainDirection.Increase
            ? StockMovement.StockInAtCarryingCost
            : StockMovement.StockOut;

        // Rule-20 hard block on a decrease; insufficient stock throws → rollback → 400.
        await context.MoveStockAsync(
            request.WarehouseId,
            movement,
            [(request.ProductId, request.Quantity, 0m)],
            _ => nameof(request.Quantity));

        var adjustment = new StockAdjustment
        {
            DateUtc = DateTimeOffset.UtcNow,
            WarehouseId = request.WarehouseId,
            Warehouse = null!,
            ProductId = request.ProductId,
            Product = null!,
            Direction = direction,
            Quantity = request.Quantity,
            Reason = request.Reason,
            Note = request.Note,
            UnitCost = unitCost,
            CreatedById = currentUser.UserId,
        };
        context.StockAdjustments.Add(adjustment);
        await context.SaveChangesAsync();

        // Read before the commit releases the lock: this adjustment is still the latest event for the product, so the
        // stock now is exactly its post-adjustment balance.
        var balanceAfter = await context.WarehouseItems
            .Where(i => i.WarehouseId == request.WarehouseId && i.ProductId == request.ProductId)
            .Select(i => i.Quantity)
            .FirstOrDefaultAsync();

        await write.CommitAsync();

        return await GetProjectedOrThrowAsync(adjustment.Id, balanceAfter);
    }

    private async Task<StockAdjustmentDto> GetProjectedOrThrowAsync(int id, decimal balanceAfter)
    {
        var adjustment = await WithDetails().FirstAsync(x => x.Id == id);

        return adjustment.ToDto(balanceAfter);
    }

    private IQueryable<StockAdjustment> WithDetails() =>
        context.StockAdjustments
            .Include(x => x.Warehouse)
            .Include(x => x.CreatedByUser)
            .Include(x => x.Product)
            .ThenInclude(p => p.Category)
            .IgnoreAutoIncludes()
            .AsNoTracking();
}
