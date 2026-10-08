using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Report;

namespace Ombor.Application.Services.Reports;

/// <summary>
/// The stock report: today's stock rows valued at their warehouse WAC and at the sale price. Every row with stock is
/// listed, archived warehouses and products included (rule 31); an empty row only while its product and warehouse are
/// active, so a sold-out product still shows. Low stock follows the warehouse item's own threshold (<see cref="LowStock"/>).
/// </summary>
internal sealed class StockReportBuilder(IApplicationDbContext context)
{
    public async Task<StockReportDto> BuildAsync(int? warehouseId)
    {
        var query = context.WarehouseItems.AsNoTracking();

        if (warehouseId is int id)
        {
            query = query.Where(i => i.WarehouseId == id);
        }

        var items = await query
            .Where(i => i.Quantity > 0m || (!i.Product.IsArchived && !i.Warehouse.IsArchived))
            .Select(i => new
            {
                i.WarehouseId,
                WarehouseName = i.Warehouse.Name,
                WarehouseIsArchived = i.Warehouse.IsArchived,
                i.ProductId,
                ProductName = i.Product.Name,
                i.Product.SKU,
                CategoryName = i.Product.Category.Name,
                i.Product.Measurement,
                ProductIsArchived = i.Product.IsArchived,
                i.Quantity,
                i.AverageCost,
                i.Product.SalePrice,
                i.LowStockThreshold,
            })
            .ToArrayAsync();

        var rows = items
            .OrderBy(i => i.WarehouseName, StringComparer.Ordinal)
            .ThenBy(i => i.ProductName, StringComparer.Ordinal)
            .Select(i => new StockReportRowDto(
                i.WarehouseId,
                i.WarehouseName,
                i.WarehouseIsArchived,
                i.ProductId,
                i.ProductName,
                i.SKU,
                i.CategoryName,
                i.Measurement.ToString(),
                i.ProductIsArchived,
                i.Quantity,
                i.AverageCost,
                Money(i.Quantity * i.AverageCost),
                i.SalePrice,
                Money(i.Quantity * i.SalePrice),
                i.LowStockThreshold,
                LowStock.IsLowStock(i.Quantity, i.LowStockThreshold, i.ProductIsArchived, i.WarehouseIsArchived)))
            .ToArray();

        // Values are summed unrounded and rounded once, the dashboard's stock-value rule.
        var warehouses = items
            .GroupBy(i => i.WarehouseId)
            .Select(g => new StockReportWarehouseDto(
                g.Key,
                g.First().WarehouseName,
                g.First().WarehouseIsArchived,
                g.Count(i => i.Quantity > 0m),
                Money(g.Sum(i => i.Quantity * i.AverageCost)),
                Money(g.Sum(i => i.Quantity * i.SalePrice)),
                g.Count(i => LowStock.IsLowStock(i.Quantity, i.LowStockThreshold, i.ProductIsArchived, i.WarehouseIsArchived))))
            .OrderBy(w => w.Name, StringComparer.Ordinal)
            .ToArray();

        var totals = new StockReportTotalsDto(
            items.Where(i => i.Quantity > 0m).Select(i => i.ProductId).Distinct().Count(),
            Money(items.Sum(i => i.Quantity * i.AverageCost)),
            Money(items.Sum(i => i.Quantity * i.SalePrice)),
            rows.Count(r => r.IsLowStock));

        return new StockReportDto(rows, warehouses, totals);
    }

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
