using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

/// <summary>
/// Moves a transaction's stock and snapshots each line's unit cost in the same write (scope-9), so profit for a past
/// period never depends on today's WAC: a Sale and a SupplyRefund record the WAC the goods left at, a Supply its net
/// purchase cost (the cost that enters the WAC), and a SaleRefund the original sale's cost — the cost the returned goods
/// are restocked at. Shared by transaction create and order delivery. The caller holds the organization write lock.
/// </summary>
internal sealed class TransactionStock(IApplicationDbContext context)
{
    /// <summary>Moves the stock for <paramref name="transaction"/>'s lines and stamps their cost.</summary>
    /// <param name="transaction">The new transaction, with its lines resolved to base units.</param>
    /// <param name="original">For a SaleRefund, the original sale with its lines.</param>
    public async Task MoveAsync(TransactionRecord transaction, TransactionRecord? original = null)
    {
        var lines = transaction.Lines.ToArray();
        var incoming = transaction.Type switch
        {
            TransactionType.Supply => lines.Select(NetUnitCost).ToArray(),
            TransactionType.SaleRefund => await RefundCostsAsync(transaction, lines, original),
            _ => new decimal[lines.Length],
        };

        var movedAt = await context.MoveStockAsync(
            transaction.WarehouseId,
            transaction.Type.ToStockMovement(),
            lines.Select((line, i) => (line.ProductId, line.Quantity, incoming[i])));

        for (var i = 0; i < lines.Length; i++)
        {
            var unitCost = transaction.Type is TransactionType.Supply or TransactionType.SaleRefund
                ? incoming[i]
                : movedAt[lines[i].ProductId];
            lines[i].UnitCost = Math.Round(unitCost, 2, MidpointRounding.AwayFromZero);
        }
    }

    // The discount lowers what the goods cost; the line total already applies it (rule 37).
    private static decimal NetUnitCost(TransactionLine line) =>
        line.Quantity == 0m ? 0m : line.Total / line.Quantity;

    /// <summary>
    /// Per refund line, the original sale's cost of that product (quantity-weighted over its lines). A refund of an
    /// estimated cost is estimated too. An original without a cost (a row planted outside the write path) falls back to
    /// the backfill's estimate (<c>TransactionLineCostBackfill</c>): this warehouse's WAC, then the product's
    /// organization-wide average, then its supply price.
    /// </summary>
    private async Task<decimal[]> RefundCostsAsync(TransactionRecord refund, TransactionLine[] lines, TransactionRecord? original)
    {
        var originalLines = (original?.Lines ?? []).GroupBy(l => l.ProductId).ToDictionary(g => g.Key, g => g.ToArray());
        var costs = new decimal[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            originalLines.TryGetValue(lines[i].ProductId, out var sold);

            if (sold is { Length: > 0 } && sold.All(l => l.UnitCost.HasValue) && sold.Sum(l => l.Quantity) is > 0m and var soldQuantity)
            {
                costs[i] = sold.Sum(l => l.UnitCost!.Value * l.Quantity) / soldQuantity;
                lines[i].CostIsEstimated = sold.Any(l => l.CostIsEstimated);
                continue;
            }

            costs[i] = await EstimateCostAsync(refund.WarehouseId, lines[i].ProductId);
            lines[i].CostIsEstimated = true;
        }

        return costs;
    }

    private async Task<decimal> EstimateCostAsync(int warehouseId, int productId)
    {
        var items = await context.WarehouseItems
            .AsNoTracking()
            .Where(i => i.ProductId == productId && i.AverageCost > 0m)
            .Select(i => new { i.WarehouseId, i.Quantity, i.AverageCost })
            .ToArrayAsync();

        if (items.FirstOrDefault(i => i.WarehouseId == warehouseId) is { } here)
        {
            return here.AverageCost;
        }

        var stocked = items.Where(i => i.Quantity > 0m).ToArray();
        if (stocked.Length > 0)
        {
            return stocked.Sum(i => i.Quantity * i.AverageCost) / stocked.Sum(i => i.Quantity);
        }

        if (items.Length > 0)
        {
            return items.Average(i => i.AverageCost);
        }

        return await context.Products
            .Where(p => p.Id == productId)
            .Select(p => p.SupplyPrice)
            .FirstOrDefaultAsync();
    }
}
