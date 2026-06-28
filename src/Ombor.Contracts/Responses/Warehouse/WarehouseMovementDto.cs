using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Warehouse;

/// <summary>
/// One stock movement in a warehouse's ledger, with the running per-product balance after it.
/// </summary>
/// <param name="Id">The source event id.</param>
/// <param name="Date">When the movement happened.</param>
/// <param name="Kind">The specific event type the movement came from — sourced from the underlying event, not the stock direction.</param>
/// <param name="ProductId">The product moved.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Measurement">The product's unit of measurement.</param>
/// <param name="Counterparty">The partner or other warehouse involved, if any.</param>
/// <param name="Note">Optional note.</param>
/// <param name="Quantity">The signed quantity delta (positive = in, negative = out).</param>
/// <param name="BalanceAfter">The product's stock in this warehouse after the movement.</param>
public sealed record WarehouseMovementDto(
    int Id,
    DateTimeOffset Date,
    MovementKind Kind,
    int ProductId,
    string ProductName,
    string Measurement,
    string? Counterparty,
    string? Note,
    int Quantity,
    int BalanceAfter);
