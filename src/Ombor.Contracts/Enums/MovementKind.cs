using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// The specific event type a stock movement came from, as surfaced in the product and warehouse
/// movement ledgers. A closed set sourced from the underlying event/transaction type — never inferred
/// from the stock direction — so an audit consumer can rely on it without guessing from the quantity sign.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MovementKind
{
    /// <summary>Initial stock recorded when a product is first stocked in a warehouse.</summary>
    Opening = 1,

    /// <summary>Stock-in from a supply transaction.</summary>
    Supply = 2,

    /// <summary>Stock-out from a sale transaction.</summary>
    Sale = 3,

    /// <summary>Stock-in from a sale refund (goods returned by a customer).</summary>
    SaleRefund = 4,

    /// <summary>Stock-out from a supply refund (goods returned to a supplier).</summary>
    SupplyRefund = 5,

    /// <summary>Stock-in or stock-out from a manual stock adjustment (direction conveyed by the quantity sign).</summary>
    Adjustment = 6,

    /// <summary>Stock-out at the source or stock-in at the destination of a transfer (direction conveyed by the quantity sign).</summary>
    Transfer = 7,
}
