using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Transfer;
using Ombor.Contracts.Responses.Transfer;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class TransferService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser) : ITransferService
{
    public async Task<TransferDto[]> GetAsync(GetTransfersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery();

        if (request.WarehouseId.HasValue)
        {
            var warehouseId = request.WarehouseId.Value;
            query = query.Where(t => t.FromWarehouseId == warehouseId || t.ToWarehouseId == warehouseId);
        }

        var transfers = await query
            .OrderByDescending(t => t.DateUtc)
            .ThenByDescending(t => t.Id)
            .ToArrayAsync();

        return [.. transfers.Select(x => x.ToDto())];
    }

    public async Task<TransferDto> GetByIdAsync(GetTransferByIdRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = await GetQuery()
            .FirstOrDefaultAsync(t => t.Id == request.Id)
            ?? throw new EntityNotFoundException<Transfer>(request.Id);

        return transfer.ToDto();
    }

    public async Task<TransferDto> CreateAsync(CreateTransferRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            throw new ValidationException("Source and destination warehouses must be different.");
        }

        await EnsureWarehouseExistsAsync(request.FromWarehouseId, "Source");
        await EnsureWarehouseExistsAsync(request.ToWarehouseId, "Destination");

        var lines = request.Lines
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToArray();
        var productIds = lines.Select(l => l.ProductId).ToArray();

        // The goods move at the source's carrying cost — capture it before the send decrements stock.
        var sourceWac = await context.WarehouseItems
            .Where(i => i.WarehouseId == request.FromWarehouseId && productIds.Contains(i.ProductId))
            .ToDictionaryAsync(i => i.ProductId, i => i.AverageCost);

        // Send (rule-20 hard block) then receive (WAC-weighted at the source cost). A single SaveChanges
        // moves both warehouses atomically; the hard block throws before any write on insufficient stock.
        await context.MoveStockAsync(
            request.FromWarehouseId,
            StockMovement.StockOut,
            lines.Select(l => (l.ProductId, l.Quantity, 0m)));

        await context.MoveStockAsync(
            request.ToWarehouseId,
            StockMovement.StockInWeightedAverage,
            lines.Select(l => (l.ProductId, l.Quantity, sourceWac.TryGetValue(l.ProductId, out var cost) ? cost : 0m)));

        var transfer = new Transfer
        {
            FromWarehouseId = request.FromWarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            DateUtc = DateTimeOffset.UtcNow,
            Notes = request.Note,
            CreatedBy = currentUser.UserId?.ToString(),
            FromWarehouse = null!,
            ToWarehouse = null!,
            Lines = [.. lines.Select(l => new TransferLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                Product = null!,
                Transfer = null!,
            })],
        };

        context.Transfers.Add(transfer);
        await context.SaveChangesAsync();

        return await GetByIdAsync(new GetTransferByIdRequest(transfer.Id));
    }

    private IQueryable<Transfer> GetQuery() =>
        context.Transfers
            .IgnoreAutoIncludes()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Lines)
            .ThenInclude(l => l.Product)
            .AsNoTracking();

    private async Task EnsureWarehouseExistsAsync(int warehouseId, string role)
    {
        if (!await context.Warehouses.AnyAsync(i => i.Id == warehouseId))
        {
            throw new ValidationException($"{role} warehouse {warehouseId} does not exist.");
        }
    }
}
