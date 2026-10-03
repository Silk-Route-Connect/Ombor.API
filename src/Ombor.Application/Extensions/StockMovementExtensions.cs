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
    /// <summary>Stock-in that recomputes WAC from the incoming value (Supply).</summary>
    StockInWeightedAverage,

    /// <summary>
    /// Stock-in at the existing carrying cost — quantity only, no WAC recompute (SaleRefund re-entry,
    /// stock-adjustment increase).
    /// </summary>
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
        TransactionType.SaleRefund => StockMovement.StockInAtCarryingCost,
        TransactionType.Sale or TransactionType.SupplyRefund => StockMovement.StockOut,
        _ => throw new InvalidOperationException($"No stock movement for transaction type {type}."),
    };

    /// <summary>
    /// Applies a stock movement to the <see cref="WarehouseItem"/> rows of one warehouse — the single
    /// stock path shared by transactions, order delivery, and stock adjustments. <see cref="WarehouseItem"/>
    /// is the sole source of truth for stock; WAC is recomputed only on a weighted-average stock-in.
    /// Negative stock is hard-blocked (rule 20) with a <c>stock.insufficient</c> 400 on the offending line's
    /// property (<paramref name="propertyFor"/> maps the line's index; defaults to <c>Lines[i].Quantity</c>).
    /// The caller owns the surrounding transaction.
    /// </summary>
    public static async Task MoveStockAsync(
        this IApplicationDbContext context,
        int warehouseId,
        StockMovement movement,
        IEnumerable<(int ProductId, decimal Quantity, decimal UnitPrice)> rawLines,
        Func<int, string>? propertyFor = null)
    {
        var lines = rawLines
            .Select((line, index) => (line.ProductId, line.Quantity, line.UnitPrice, Index: index))
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(l => l.Quantity),
                IncomingValue = g.Sum(l => l.Quantity * l.UnitPrice),
                FirstIndex = g.First().Index,
            })
            .ToArray();
        var productIds = lines.Select(x => x.ProductId).ToArray();

        var items = await context.WarehouseItems
            .Where(x => x.WarehouseId == warehouseId && productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId);

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
            }
            else
            {
                // StockInAtCarryingCost: re-enter at the existing carrying cost (no WAC recompute).
                item.Quantity += line.Quantity;
            }
        }
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
