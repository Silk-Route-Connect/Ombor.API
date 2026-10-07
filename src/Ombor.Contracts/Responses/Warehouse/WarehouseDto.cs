namespace Ombor.Contracts.Responses.Warehouse;

/// <summary>A warehouse with its computed stock totals.</summary>
/// <param name="Id">The warehouse id.</param>
/// <param name="Name">The warehouse name.</param>
/// <param name="Location">The warehouse location, if any.</param>
/// <param name="ProductCount">Number of distinct products on hand (quantity above zero; an emptied stock row is not counted).</param>
/// <param name="LowStockCount">
/// «Заканчивается»: stock rows that have a low-stock threshold and hold that much or less (DR-41) — the rows the stock
/// tab flags <c>IsLowStock</c>; 0 for an archived warehouse. There is no unit total: different products are never summed.
/// </param>
/// <param name="StockValue">Total stock value (Σ quantity × average cost).</param>
/// <param name="IsArchived">Whether the warehouse is archived (still counts in totals).</param>
/// <param name="IsDeletable">True when no other record references the warehouse (otherwise DELETE returns 409).</param>
public sealed record WarehouseDto(
    int Id,
    string Name,
    string? Location,
    int ProductCount,
    int LowStockCount,
    decimal StockValue,
    bool IsArchived,
    bool IsDeletable);
