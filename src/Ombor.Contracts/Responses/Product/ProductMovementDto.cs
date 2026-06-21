namespace Ombor.Contracts.Responses.Product;

/// <summary>
/// One stock movement for a product across warehouses, with the running total-stock balance after it.
/// </summary>
/// <param name="Id">The source event id.</param>
/// <param name="ProductId">The product moved.</param>
/// <param name="Date">When the movement happened.</param>
/// <param name="Kind">Opening, Supply, Sale, Refund, Adjustment, or Transfer.</param>
/// <param name="WarehouseId">The warehouse the movement happened in.</param>
/// <param name="WarehouseName">The warehouse name.</param>
/// <param name="Quantity">The signed quantity delta (positive = in, negative = out).</param>
/// <param name="BalanceAfter">The product's total stock across all warehouses after the movement.</param>
public sealed record ProductMovementDto(
    int Id,
    int ProductId,
    DateTimeOffset Date,
    string Kind,
    int WarehouseId,
    string WarehouseName,
    int Quantity,
    int BalanceAfter);
