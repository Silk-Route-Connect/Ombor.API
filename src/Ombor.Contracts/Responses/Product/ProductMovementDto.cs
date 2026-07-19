using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Product;

/// <summary>
/// One stock movement for a product across warehouses, with the running total-stock balance after it.
/// </summary>
/// <param name="Id">The source event id.</param>
/// <param name="ProductId">The product moved.</param>
/// <param name="Date">When the movement happened.</param>
/// <param name="Kind">The specific event type the movement came from — sourced from the underlying event, not the stock direction.</param>
/// <param name="WarehouseId">The warehouse the movement happened in.</param>
/// <param name="WarehouseName">The warehouse name.</param>
/// <param name="CounterpartyWarehouseId">For a transfer row, the id of the other warehouse (the deep-link target that ties the transfer's send and receive rows together); otherwise null.</param>
/// <param name="CounterpartyWarehouseName">For a transfer row, the name of the other warehouse; otherwise null.</param>
/// <param name="Quantity">The signed quantity delta (positive = in, negative = out).</param>
/// <param name="BalanceAfter">The product's total stock across all warehouses after the movement.</param>
public sealed record ProductMovementDto(
    int Id,
    int ProductId,
    DateTimeOffset Date,
    MovementKind Kind,
    int WarehouseId,
    string WarehouseName,
    int? CounterpartyWarehouseId,
    string? CounterpartyWarehouseName,
    decimal Quantity,
    decimal BalanceAfter);
