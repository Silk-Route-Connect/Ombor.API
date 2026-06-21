using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class StockMovementExtensions
{
    /// <summary>
    /// Applies a stock movement to the <see cref="WarehouseItem"/> rows of one warehouse — the single
    /// stock path shared by transaction creation and order-delivery promotion. <see cref="WarehouseItem"/>
    /// is the sole source of truth for stock; weighted-average cost is recomputed on every Supply stock-in.
    /// Negative stock is hard-blocked (rule 20). The caller owns the surrounding transaction.
    /// </summary>
    public static async Task MoveStockAsync(
        this IApplicationDbContext context,
        int warehouseId,
        TransactionType domainType,
        IEnumerable<(int ProductId, int Quantity, decimal UnitPrice)> rawLines)
    {
        var isStockIn = domainType is TransactionType.Supply or TransactionType.SaleRefund;

        var lines = rawLines
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(l => l.Quantity),
                IncomingValue = g.Sum(l => l.Quantity * l.UnitPrice),
            })
            .ToArray();
        var productIds = lines.Select(x => x.ProductId).ToArray();

        var items = await context.WarehouseItems
            .Where(x => x.WarehouseId == warehouseId && productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId);

        foreach (var line in lines)
        {
            items.TryGetValue(line.ProductId, out var item);

            if (isStockIn)
            {
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

                if (domainType == TransactionType.Supply)
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
                    // SaleRefund: returned goods re-enter at their existing carrying cost.
                    item.Quantity += line.Quantity;
                }
            }
            else
            {
                // Stock-out: Sale, SupplyRefund. Negative stock is hard-blocked.
                if (item is null || item.Quantity < line.Quantity)
                {
                    throw new ValidationException(
                        $"Insufficient stock for product {line.ProductId} in the selected warehouse.");
                }

                item.Quantity -= line.Quantity;
            }
        }
    }
}
