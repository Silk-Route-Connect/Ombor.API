using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

/// <summary>
/// The served <c>balanceAfter</c> of stock adjustments: the product's stock in the adjustment's warehouse right after
/// it. Folds only the stock events of the (warehouse, product) pairs asked for — not whole warehouse ledgers — in the
/// order the warehouse movement ledger uses (date, then id, then openings / transaction lines / adjustments /
/// transfers), so the figure always equals that ledger's balance for the same row.
/// </summary>
internal sealed class StockAdjustmentBalances(IApplicationDbContext context)
{
    public async Task<Dictionary<int, decimal>> ComputeAsync(IReadOnlyCollection<AdjustmentKey> adjustments)
    {
        if (adjustments.Count == 0)
        {
            return [];
        }

        var pairs = adjustments.Select(a => (a.WarehouseId, a.ProductId)).ToHashSet();
        var warehouseIds = pairs.Select(p => p.WarehouseId).Distinct().ToArray();
        var productIds = pairs.Select(p => p.ProductId).Distinct().ToArray();

        // The id filters select a superset (every warehouse × every product); the pair check below narrows it.
        var openings = await context.OpeningStocks
            .AsNoTracking()
            .Where(o => warehouseIds.Contains(o.WarehouseId) && productIds.Contains(o.ProductId))
            .Select(o => new StockEvent(o.Id, o.DateUtc, Source.Opening, o.WarehouseId, o.ProductId, o.Quantity))
            .ToListAsync();

        var lines = await context.TransactionLines
            .AsNoTracking()
            .Where(l => warehouseIds.Contains(l.Transaction.WarehouseId) && productIds.Contains(l.ProductId))
            .Select(l => new { l.Id, l.Transaction.DateUtc, l.Transaction.WarehouseId, l.ProductId, l.Quantity, l.Transaction.Type })
            .ToListAsync();

        var adjustmentRows = await context.StockAdjustments
            .AsNoTracking()
            .Where(a => warehouseIds.Contains(a.WarehouseId) && productIds.Contains(a.ProductId))
            .Select(a => new StockEvent(
                a.Id, a.DateUtc, Source.Adjustment, a.WarehouseId, a.ProductId,
                a.Direction == StockAdjustmentDirection.Increase ? a.Quantity : -a.Quantity))
            .ToListAsync();

        var transferLines = await context.TransferLines
            .AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && (warehouseIds.Contains(l.Transfer.FromWarehouseId) || warehouseIds.Contains(l.Transfer.ToWarehouseId)))
            .Select(l => new { l.Id, l.Transfer.DateUtc, l.Transfer.FromWarehouseId, l.Transfer.ToWarehouseId, l.ProductId, l.Quantity })
            .ToListAsync();

        var events = openings
            .Concat(lines.Select(l => new StockEvent(
                l.Id, l.DateUtc, Source.TransactionLine, l.WarehouseId, l.ProductId,
                l.Type is TransactionType.Supply or TransactionType.SaleRefund ? l.Quantity : -l.Quantity)))
            .Concat(adjustmentRows)
            .Concat(transferLines.SelectMany(l => new[]
            {
                new StockEvent(l.Id, l.DateUtc, Source.Transfer, l.FromWarehouseId, l.ProductId, -l.Quantity),
                new StockEvent(l.Id, l.DateUtc, Source.Transfer, l.ToWarehouseId, l.ProductId, l.Quantity),
            }))
            .Where(e => pairs.Contains((e.WarehouseId, e.ProductId)));

        var wanted = adjustments.Select(a => a.Id).ToHashSet();
        var running = new Dictionary<(int, int), decimal>();
        var balances = new Dictionary<int, decimal>();

        foreach (var e in events.OrderBy(e => e.Date).ThenBy(e => e.Id).ThenBy(e => e.Source))
        {
            var pair = (e.WarehouseId, e.ProductId);
            var balance = running.GetValueOrDefault(pair) + e.Quantity;
            running[pair] = balance;

            if (e.Source == Source.Adjustment && wanted.Contains(e.Id))
            {
                balances[e.Id] = balance;
            }
        }

        return balances;
    }

    // Declared in the order the warehouse ledger lists its sources, which breaks its date + id ties.
    private enum Source
    {
        Opening,
        TransactionLine,
        Adjustment,
        Transfer,
    }

    private sealed record StockEvent(int Id, DateTimeOffset Date, Source Source, int WarehouseId, int ProductId, decimal Quantity);
}

/// <summary>An adjustment whose balance is asked for, with the stock it moved.</summary>
internal sealed record AdjustmentKey(int Id, int WarehouseId, int ProductId);
