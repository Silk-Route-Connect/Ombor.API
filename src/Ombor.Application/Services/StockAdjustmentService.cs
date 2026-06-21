using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.StockAdjustment;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Domain.Entities;
using DomainDirection = Ombor.Domain.Enums.StockAdjustmentDirection;

namespace Ombor.Application.Services;

internal sealed class StockAdjustmentService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser) : IStockAdjustmentService
{
    public async Task<StockAdjustmentDto[]> GetAsync(GetStockAdjustmentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.StockAdjustments
            .Include(x => x.Warehouse)
            .Include(x => x.Product)
            .ThenInclude(p => p.Category)
            .IgnoreAutoIncludes()
            .AsNoTracking();

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

        return [.. adjustments.Select(x => x.ToDto())];
    }

    public async Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        if (!await context.Warehouses.AnyAsync(w => w.Id == request.WarehouseId))
        {
            throw new ValidationException($"Warehouse {request.WarehouseId} does not exist.");
        }

        if (!await context.Products.AnyAsync(p => p.Id == request.ProductId))
        {
            throw new ValidationException($"Product {request.ProductId} does not exist.");
        }

        var direction = request.Direction.ToDomainDirection();

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // A decrease records the loss at the current carrying cost — snapshot the WAC before moving stock.
            var unitCost = 0m;
            if (direction == DomainDirection.Decrease)
            {
                unitCost = await context.WarehouseItems
                    .Where(i => i.WarehouseId == request.WarehouseId && i.ProductId == request.ProductId)
                    .Select(i => i.AverageCost)
                    .FirstOrDefaultAsync();
            }

            var movement = direction == DomainDirection.Increase
                ? StockMovement.StockInAtCarryingCost
                : StockMovement.StockOut;

            // Rule-20 hard block on a decrease; insufficient stock throws → rollback → 400.
            await context.MoveStockAsync(
                request.WarehouseId,
                movement,
                [(request.ProductId, request.Quantity, 0m)]);

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
                CreatedBy = currentUser.UserId?.ToString(),
            };
            context.StockAdjustments.Add(adjustment);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return await GetProjectedOrThrowAsync(adjustment.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<StockAdjustmentDto> GetProjectedOrThrowAsync(int id)
    {
        var adjustment = await context.StockAdjustments
            .Include(x => x.Warehouse)
            .Include(x => x.Product)
            .ThenInclude(p => p.Category)
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .FirstAsync(x => x.Id == id);

        return adjustment.ToDto();
    }
}
