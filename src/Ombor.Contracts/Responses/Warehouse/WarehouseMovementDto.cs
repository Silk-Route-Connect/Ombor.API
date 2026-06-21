namespace Ombor.Contracts.Responses.Warehouse;

/// <summary>
/// One stock movement in a warehouse's ledger, with the running per-product balance after it.
/// </summary>
/// <param name="Id">The source event id.</param>
/// <param name="Date">When the movement happened.</param>
/// <param name="Kind">Opening, Supply, Sale, Refund, Adjustment, or Transfer.</param>
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
    string Kind,
    int ProductId,
    string ProductName,
    string Measurement,
    string? Counterparty,
    string? Note,
    int Quantity,
    int BalanceAfter);
