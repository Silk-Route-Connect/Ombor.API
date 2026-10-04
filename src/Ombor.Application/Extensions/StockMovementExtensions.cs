using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Validators;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Extensions;

/// <summary>
/// How a stock movement affects a warehouse item's quantity and weighted-average cost.
/// </summary>
internal enum StockMovement
{
    /// <summary>
    /// Stock-in that recomputes WAC from the incoming cost (Supply at its net purchase cost, SaleRefund at the original
    /// sale's cost, transfer receipt at the source WAC, opening stock).
    /// </summary>
    StockInWeightedAverage,

    /// <summary>Stock-in at the existing carrying cost — quantity only, no WAC recompute (stock-adjustment increase).</summary>
    StockInAtCarryingCost,

    /// <summary>Stock-out at WAC; hard-blocks below zero (Sale, SupplyRefund, stock-adjustment decrease).</summary>
    StockOut,
}

internal static class StockMovementExtensions
{
    /// <summary>Maps a transaction type to the stock movement (cost policy) it applies.</summary>
    public static StockMovement ToStockMovement(this TransactionType type) => type switch
    {
        TransactionType.Supply => StockMovement.StockInWeightedAverage,
        // Returned goods come back at what they cost when sold (TransactionStock), so a refund into a warehouse
        // without the product, or at a different WAC, still carries its true cost.
        TransactionType.SaleRefund => StockMovement.StockInWeightedAverage,
        TransactionType.Sale or TransactionType.SupplyRefund => StockMovement.StockOut,
        _ => throw new InvalidOperationException($"No stock movement for transaction type {type}."),
    };

    /// <summary>
    /// Applies a stock movement to the <see cref="WarehouseItem"/> rows of one warehouse — the single
    /// stock path shared by transactions, order delivery, and stock adjustments. <see cref="WarehouseItem"/>
    /// is the sole source of truth for stock; WAC is recomputed only on a weighted-average stock-in.
    /// Negative stock is hard-blocked (rule 20) with a <c>stock.insufficient</c> 400 on the offending line's
    /// property (<paramref name="propertyFor"/> maps the line's index; defaults to <c>Lines[i].Quantity</c>).
    /// Each line's <c>UnitCost</c> is the incoming cost of a weighted-average stock-in and ignored otherwise.
    /// The caller owns the surrounding transaction.
    /// </summary>
    /// <returns>
    /// The unit cost each product moved at, by product id: the WAC a stock-out left at, the carrying cost of a
    /// carrying-cost stock-in, the (quantity-weighted) incoming cost of a weighted-average stock-in.
    /// </returns>
    public static async Task<IReadOnlyDictionary<int, decimal>> MoveStockAsync(
        this IApplicationDbContext context,
        int warehouseId,
        StockMovement movement,
        IEnumerable<(int ProductId, decimal Quantity, decimal UnitCost)> rawLines,
        Func<int, string>? propertyFor = null)
    {
        var lines = rawLines
            .Select((line, index) => (line.ProductId, line.Quantity, line.UnitCost, Index: index))
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(l => l.Quantity),
                IncomingValue = g.Sum(l => l.Quantity * l.UnitCost),
                FirstIndex = g.First().Index,
            })
            .ToArray();
        var productIds = lines.Select(x => x.ProductId).ToArray();

        var items = await context.WarehouseItems
            .Where(x => x.WarehouseId == warehouseId && productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId);
        var movedAt = new Dictionary<int, decimal>(lines.Length);

        foreach (var line in lines)
        {
            items.TryGetValue(line.ProductId, out var item);

            if (movement == StockMovement.StockOut)
            {
                // Stock-out leaves at WAC; negative stock is hard-blocked.
                if (item is null || item.Quantity < line.Quantity)
                {
                    throw await InsufficientStockAsync(
                        context,
                        line.ProductId,
                        available: item?.Quantity ?? 0m,
                        requested: line.Quantity,
                        (propertyFor ?? DefaultLineProperty)(line.FirstIndex));
                }

                movedAt[line.ProductId] = item.AverageCost;
                item.Quantity -= line.Quantity;
                continue;
            }

            if (item is null)
            {
                item = new WarehouseItem
                {
                    WarehouseId = warehouseId,
                    ProductId = line.ProductId,
                    Quantity = 0,
                    AverageCost = 0m,
                    Warehouse = null!,
                    Product = null!,
                };
                context.WarehouseItems.Add(item);
            }

            if (movement == StockMovement.StockInWeightedAverage)
            {
                // Weighted-average cost recompute on stock-in (rules.md #10).
                var newQuantity = item.Quantity + line.Quantity;
                item.AverageCost = newQuantity == 0
                    ? 0m
                    : ((item.Quantity * item.AverageCost) + line.IncomingValue) / newQuantity;
                item.Quantity = newQuantity;
                movedAt[line.ProductId] = line.Quantity == 0 ? 0m : line.IncomingValue / line.Quantity;
            }
            else
            {
                // StockInAtCarryingCost: re-enter at the existing carrying cost (no WAC recompute).
                item.Quantity += line.Quantity;
                movedAt[line.ProductId] = item.AverageCost;
            }
        }

        return movedAt;
    }

    private static string DefaultLineProperty(int index) => $"Lines[{index}].Quantity";

    private static async Task<ValidationException> InsufficientStockAsync(
        IApplicationDbContext context,
        int productId,
        decimal available,
        decimal requested,
        string propertyName)
    {
        var productName = await context.Products
            .Where(p => p.Id == productId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync();

        return CodedValidation.Failure(
            propertyName,
            $"Insufficient stock for '{productName}' in the selected warehouse. Available: {available}, requested: {requested}.",
            ErrorCodes.StockInsufficient,
            new Dictionary<string, object?>
            {
                ["productName"] = productName,
                ["available"] = available,
                ["requested"] = requested,
            });
    }
}
