namespace Ombor.Contracts.Responses.StockAdjustment;

/// <summary>A recorded stock adjustment.</summary>
/// <param name="Id">The adjustment id.</param>
/// <param name="Date">When the adjustment happened.</param>
/// <param name="WarehouseId">The warehouse adjusted.</param>
/// <param name="WarehouseName">The warehouse name.</param>
/// <param name="ProductId">The product adjusted.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Sku">The product SKU.</param>
/// <param name="CategoryName">The product's category name, if any.</param>
/// <param name="Measurement">The product's unit of measurement.</param>
/// <param name="Direction">Increase or Decrease.</param>
/// <param name="Quantity">The adjusted quantity (positive magnitude).</param>
/// <param name="Reason">The adjustment reason.</param>
/// <param name="Note">Optional free-text note.</param>
/// <param name="CreatedBy">The user who made the adjustment, if known.</param>
/// <param name="BalanceAfter">The product's stock in this warehouse immediately after the adjustment.</param>
/// <param name="UnitCost">
/// The weighted-average unit cost snapshotted at the adjustment — the loss cost of a Decrease, the carrying cost a
/// unit is restored at on an Increase. 0 on Increase rows recorded before 2026-10-04, which did not snapshot it.
/// </param>
/// <param name="Value">
/// <c>Quantity × UnitCost</c>, rounded to the tiyin: on a Decrease, the amount written off (a loss line distinct from
/// COGS); on an Increase, the value restored.
/// </param>
public sealed record StockAdjustmentDto(
    int Id,
    DateTimeOffset Date,
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    string Sku,
    string? CategoryName,
    string Measurement,
    string Direction,
    decimal Quantity,
    string Reason,
    string? Note,
    string? CreatedBy,
    decimal BalanceAfter,
    decimal UnitCost,
    decimal Value);
