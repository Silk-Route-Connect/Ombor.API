using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>The direction of a stock adjustment (business-rules rule 24).</summary>
[JsonConverter(typeof(Serialization.ValidatingStringEnumConverter))]
public enum StockAdjustmentDirection
{
    /// <summary>Stock removed (loss/correction) — recorded at WAC as a loss.</summary>
    Decrease = 1,

    /// <summary>Stock added (found/correction) — audited stock-in at current WAC.</summary>
    Increase = 2,
}
