using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// The kind of document a stock movement row belongs to — the target a client opens from the row. Several
/// <see cref="MovementKind"/> values share one source (every sale, supply and refund row is a
/// <see cref="Transaction"/>).
/// </summary>
[JsonConverter(typeof(Serialization.ValidatingStringEnumConverter))]
public enum MovementSource
{
    /// <summary>A sale, supply or refund document (<c>/api/transactions/{id}</c>); carries a document number.</summary>
    Transaction = 1,

    /// <summary>An inter-warehouse transfer (<c>/api/transfers/{id}</c>); no document number.</summary>
    Transfer = 2,

    /// <summary>A stock adjustment (<c>/api/stock-adjustments</c>); no document number.</summary>
    StockAdjustment = 3,

    /// <summary>An opening-stock record of a warehouse; no document number.</summary>
    OpeningStock = 4,
}
