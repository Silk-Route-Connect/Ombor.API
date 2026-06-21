namespace Ombor.Contracts.Requests.Warehouse;

/// <summary>
/// Request to record the opening (initial) stock of a warehouse. Each item creates a new
/// warehouse item; products already stocked in the warehouse are rejected.
/// </summary>
/// <param name="WarehouseId">The warehouse the stock belongs to.</param>
/// <param name="Items">The products, quantities and unit costs to stock in.</param>
public sealed record AddOpeningStockRequest(
    int WarehouseId,
    OpeningStockLine[] Items);

/// <param name="ProductId">The product being stocked.</param>
/// <param name="Quantity">The opening quantity (must be &gt; 0).</param>
/// <param name="UnitCost">The cost per unit, used as the initial weighted-average cost.</param>
public sealed record OpeningStockLine(int ProductId, int Quantity, decimal UnitCost);
