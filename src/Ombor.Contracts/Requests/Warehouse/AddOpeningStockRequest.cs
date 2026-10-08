namespace Ombor.Contracts.Requests.Warehouse;

/// <summary>
/// Request to record the opening (initial) stock of a warehouse. Each item creates a new
/// warehouse item; products already stocked in the warehouse are rejected.
/// </summary>
/// <param name="WarehouseId">The warehouse the stock belongs to.</param>
/// <param name="Items">The products, quantities and unit costs to stock in.</param>
/// <param name="Note">Optional free-text audit note recorded on the opening-stock event.</param>
public sealed record AddOpeningStockRequest(
    int WarehouseId,
    OpeningStockLine[] Items,
    string? Note = null);

/// <summary>One product of an opening stock.</summary>
/// <param name="ProductId">The product being stocked.</param>
/// <param name="Quantity">The opening quantity (must be &gt; 0).</param>
/// <param name="UnitCost">The cost per unit, used as the initial weighted-average cost.</param>
/// <param name="LowStockThreshold">
/// Optional low-stock threshold of the new stock row (DR-41, ≥ 0): the row counts as «Заканчивается» at or below it;
/// <see langword="null"/> leaves the row untracked.
/// </param>
public sealed record OpeningStockLine(int ProductId, decimal Quantity, decimal UnitCost, decimal? LowStockThreshold = null);
