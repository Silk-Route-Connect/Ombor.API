using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Product;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class MovementService(IApplicationDbContext context) : IMovementService
{
    public async Task<WarehouseMovementDto[]> GetWarehouseMovementsAsync(int warehouseId)
    {
        if (!await context.Warehouses.AnyAsync(w => w.Id == warehouseId))
        {
            throw new EntityNotFoundException<Warehouse>(warehouseId);
        }

        var movements = new List<Raw>();

        var openings = await context.OpeningStocks
            .Where(o => o.WarehouseId == warehouseId)
            .Select(o => new { o.Id, o.DateUtc, o.ProductId, ProductName = o.Product.Name, o.Product.Measurement, o.Quantity })
            .ToListAsync();
        movements.AddRange(openings.Select(o => new Raw(
            o.Id, o.DateUtc, MovementKinds.Opening, o.ProductId, o.ProductName, o.Measurement.ToString(), warehouseId, string.Empty, null, null, o.Quantity)));

        var transactionLines = await context.TransactionLines
            .Where(l => l.Transaction.WarehouseId == warehouseId)
            .Select(l => new { l.Id, l.Transaction.DateUtc, l.Transaction.Type, l.ProductId, ProductName = l.Product.Name, l.Product.Measurement, l.Quantity, Partner = l.Transaction.Partner.Name, l.Transaction.RefundReason })
            .ToListAsync();
        movements.AddRange(transactionLines.Select(l => new Raw(
            l.Id, l.DateUtc, KindOf(l.Type), l.ProductId, l.ProductName, l.Measurement.ToString(), warehouseId, string.Empty, l.Partner, l.RefundReason, SignedOf(l.Type, (int)l.Quantity))));

        var adjustments = await context.StockAdjustments
            .Where(a => a.WarehouseId == warehouseId)
            .Select(a => new { a.Id, a.DateUtc, a.Direction, a.ProductId, ProductName = a.Product.Name, a.Product.Measurement, a.Quantity, a.Reason, a.Note })
            .ToListAsync();
        movements.AddRange(adjustments.Select(a => new Raw(
            a.Id, a.DateUtc, MovementKinds.Adjustment, a.ProductId, a.ProductName, a.Measurement.ToString(), warehouseId, string.Empty, null, a.Note ?? a.Reason,
            a.Direction == StockAdjustmentDirection.Increase ? a.Quantity : -a.Quantity)));

        var transferLines = await context.TransferLines
            .Where(l => l.Transfer.FromWarehouseId == warehouseId || l.Transfer.ToWarehouseId == warehouseId)
            .Select(l => new { l.Id, l.Transfer.DateUtc, l.Transfer.FromWarehouseId, FromName = l.Transfer.FromWarehouse.Name, l.Transfer.ToWarehouseId, ToName = l.Transfer.ToWarehouse.Name, l.ProductId, ProductName = l.Product.Name, l.Product.Measurement, l.Quantity, l.Transfer.Notes })
            .ToListAsync();
        movements.AddRange(transferLines.Select(l =>
        {
            var isSend = l.FromWarehouseId == warehouseId;
            return new Raw(
                l.Id, l.DateUtc, MovementKinds.Transfer, l.ProductId, l.ProductName, l.Measurement.ToString(), warehouseId, string.Empty,
                isSend ? l.ToName : l.FromName, l.Notes, isSend ? -(int)l.Quantity : (int)l.Quantity);
        }));

        // Running balance per product within this warehouse (reconciles to WarehouseItem.Quantity).
        var withBalance = WithRunningBalance(movements, m => m.ProductId);

        return [.. withBalance
            .Select(x => new WarehouseMovementDto(
                x.Movement.Id, x.Movement.Date, x.Movement.Kind, x.Movement.ProductId, x.Movement.ProductName,
                x.Movement.Measurement, x.Movement.Counterparty, x.Movement.Note, x.Movement.Quantity, x.Balance))];
    }

    public async Task<ProductMovementDto[]> GetProductMovementsAsync(int productId)
    {
        if (!await context.Products.AnyAsync(p => p.Id == productId))
        {
            throw new EntityNotFoundException<Product>(productId);
        }

        var movements = new List<Raw>();

        var openings = await context.OpeningStocks
            .Where(o => o.ProductId == productId)
            .Select(o => new { o.Id, o.DateUtc, o.WarehouseId, WarehouseName = o.Warehouse.Name, o.Quantity })
            .ToListAsync();
        movements.AddRange(openings.Select(o => new Raw(
            o.Id, o.DateUtc, MovementKinds.Opening, productId, string.Empty, string.Empty, o.WarehouseId, o.WarehouseName, null, null, o.Quantity)));

        var transactionLines = await context.TransactionLines
            .Where(l => l.ProductId == productId && l.Transaction.WarehouseId != null)
            .Select(l => new { l.Id, l.Transaction.DateUtc, l.Transaction.Type, WarehouseId = l.Transaction.WarehouseId!.Value, WarehouseName = l.Transaction.Warehouse!.Name, l.Quantity })
            .ToListAsync();
        movements.AddRange(transactionLines.Select(l => new Raw(
            l.Id, l.DateUtc, KindOf(l.Type), productId, string.Empty, string.Empty, l.WarehouseId, l.WarehouseName, null, null, SignedOf(l.Type, (int)l.Quantity))));

        var adjustments = await context.StockAdjustments
            .Where(a => a.ProductId == productId)
            .Select(a => new { a.Id, a.DateUtc, a.Direction, a.WarehouseId, WarehouseName = a.Warehouse.Name, a.Quantity })
            .ToListAsync();
        movements.AddRange(adjustments.Select(a => new Raw(
            a.Id, a.DateUtc, MovementKinds.Adjustment, productId, string.Empty, string.Empty, a.WarehouseId, a.WarehouseName, null, null,
            a.Direction == StockAdjustmentDirection.Increase ? a.Quantity : -a.Quantity)));

        // A transfer of this product is two movements: send at the source, receive at the destination.
        var transferLines = await context.TransferLines
            .Where(l => l.ProductId == productId)
            .Select(l => new { l.Id, l.Transfer.DateUtc, l.Transfer.FromWarehouseId, FromName = l.Transfer.FromWarehouse.Name, l.Transfer.ToWarehouseId, ToName = l.Transfer.ToWarehouse.Name, l.Quantity })
            .ToListAsync();
        foreach (var l in transferLines)
        {
            movements.Add(new Raw(l.Id, l.DateUtc, MovementKinds.Transfer, productId, string.Empty, string.Empty, l.FromWarehouseId, l.FromName, null, null, -(int)l.Quantity));
            movements.Add(new Raw(l.Id, l.DateUtc, MovementKinds.Transfer, productId, string.Empty, string.Empty, l.ToWarehouseId, l.ToName, null, null, (int)l.Quantity));
        }

        // Running total stock across all warehouses (reconciles to the product's total stock).
        var withBalance = WithRunningBalance(movements, _ => 0);

        return [.. withBalance
            .Select(x => new ProductMovementDto(
                x.Movement.Id, productId, x.Movement.Date, x.Movement.Kind, x.Movement.WarehouseId,
                x.Movement.WarehouseName, x.Movement.Quantity, x.Balance))];
    }

    /// <summary>
    /// Folds a running balance over the movements oldest→newest (per the <paramref name="bucket"/> key —
    /// per-product for a warehouse ledger, a single bucket for a product ledger), then returns newest-first.
    /// </summary>
    private static List<(Raw Movement, int Balance)> WithRunningBalance(List<Raw> movements, Func<Raw, int> bucket)
    {
        var running = new Dictionary<int, int>();

        var withBalance = movements
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Id)
            .Select(m =>
            {
                var key = bucket(m);
                running.TryGetValue(key, out var balance);
                balance += m.Quantity;
                running[key] = balance;
                return (Movement: m, Balance: balance);
            })
            .ToList();

        withBalance.Reverse();

        return withBalance;
    }

    private static string KindOf(TransactionType type) => type switch
    {
        TransactionType.Supply => MovementKinds.Supply,
        TransactionType.Sale => MovementKinds.Sale,
        _ => MovementKinds.Refund,
    };

    private static int SignedOf(TransactionType type, int quantity)
        => type is TransactionType.Supply or TransactionType.SaleRefund ? quantity : -quantity;

    private sealed record Raw(
        int Id,
        DateTimeOffset Date,
        string Kind,
        int ProductId,
        string ProductName,
        string Measurement,
        int WarehouseId,
        string WarehouseName,
        string? Counterparty,
        string? Note,
        int Quantity);
}
