namespace Ombor.Contracts.Responses.Report;

/// <summary>
/// Stock on hand now and what it is worth, per product per warehouse, with totals per warehouse and overall. Archived
/// warehouses and products still holding stock count (rule 31).
/// </summary>
/// <param name="Rows">One row per product per warehouse, by warehouse name then product name.</param>
/// <param name="Warehouses">Totals per warehouse, by name.</param>
/// <param name="Totals">Totals over every row.</param>
public sealed record StockReportDto(
    StockReportRowDto[] Rows,
    StockReportWarehouseDto[] Warehouses,
    StockReportTotalsDto Totals);

/// <summary>One product's stock in one warehouse.</summary>
/// <param name="WarehouseId">The warehouse id.</param>
/// <param name="WarehouseName">The warehouse name.</param>
/// <param name="WarehouseIsArchived">Whether the warehouse is archived.</param>
/// <param name="ProductId">The product id.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Sku">The product SKU.</param>
/// <param name="CategoryName">The product's category.</param>
/// <param name="Measurement">The product's unit of measurement.</param>
/// <param name="ProductIsArchived">Whether the product is archived.</param>
/// <param name="Quantity">Units on hand (base units).</param>
/// <param name="AverageCost">This warehouse's weighted-average cost per unit.</param>
/// <param name="Value">Quantity × average cost, 2 decimals — what the stock cost.</param>
/// <param name="SalePrice">The product's current sale price.</param>
/// <param name="SaleValue">Quantity × sale price, 2 decimals — what it would sell for.</param>
/// <param name="LowStockThreshold">This warehouse item's low-stock threshold; null when the item is not tracked.</param>
/// <param name="IsLowStock">
/// The item has a threshold and its quantity is at or below it (the warehouse stock tab's rule); always false for an
/// untracked item, an archived product or an archived warehouse.
/// </param>
public sealed record StockReportRowDto(
    int WarehouseId,
    string WarehouseName,
    bool WarehouseIsArchived,
    int ProductId,
    string ProductName,
    string Sku,
    string CategoryName,
    string Measurement,
    bool ProductIsArchived,
    decimal Quantity,
    decimal AverageCost,
    decimal Value,
    decimal SalePrice,
    decimal SaleValue,
    decimal? LowStockThreshold,
    bool IsLowStock);

/// <summary>One warehouse's stock totals.</summary>
/// <param name="WarehouseId">The warehouse id.</param>
/// <param name="Name">The warehouse name.</param>
/// <param name="IsArchived">Whether the warehouse is archived (its stock still counts).</param>
/// <param name="ProductCount">Products with stock on hand.</param>
/// <param name="Value">Σ row value — the same figure as <c>WarehouseDto.StockValue</c>.</param>
/// <param name="SaleValue">Σ row sale value.</param>
/// <param name="LowStockCount">Rows flagged <c>IsLowStock</c> — tracked rows at or below their threshold (0 for an archived warehouse).</param>
public sealed record StockReportWarehouseDto(
    int WarehouseId,
    string Name,
    bool IsArchived,
    int ProductCount,
    decimal Value,
    decimal SaleValue,
    int LowStockCount);

/// <summary>Stock totals over every listed warehouse.</summary>
/// <param name="ProductCount">Distinct products with stock on hand.</param>
/// <param name="Value">Σ value — with no warehouse filter, the dashboard's stock value.</param>
/// <param name="SaleValue">Σ sale value.</param>
/// <param name="LowStockCount">Rows flagged <c>IsLowStock</c> over every listed warehouse.</param>
public sealed record StockReportTotalsDto(
    int ProductCount,
    decimal Value,
    decimal SaleValue,
    int LowStockCount);
