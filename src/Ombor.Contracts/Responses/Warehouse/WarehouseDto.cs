namespace Ombor.Contracts.Responses.Warehouse;

/// <summary>A warehouse with its computed stock totals.</summary>
/// <param name="Id">The warehouse id.</param>
/// <param name="Name">The warehouse name.</param>
/// <param name="Location">The warehouse location, if any.</param>
/// <param name="ProductCount">Number of distinct products on hand.</param>
/// <param name="TotalUnits">Total units on hand across all products.</param>
/// <param name="StockValue">Total stock value (Σ quantity × average cost).</param>
/// <param name="IsArchived">Whether the warehouse is archived (still counts in totals).</param>
/// <param name="IsDeletable">True when no other record references the warehouse (otherwise DELETE returns 409).</param>
public sealed record WarehouseDto(
    int Id,
    string Name,
    string? Location,
    int ProductCount,
    decimal TotalUnits,
    decimal StockValue,
    bool IsArchived,
    bool IsDeletable);
