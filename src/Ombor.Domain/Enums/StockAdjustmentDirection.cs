namespace Ombor.Domain.Enums;

/// <summary>The direction of a stock adjustment (business-rules rule 24).</summary>
public enum StockAdjustmentDirection
{
    /// <summary>Stock removed (loss/correction) — recorded at WAC as a loss.</summary>
    Decrease = 1,

    /// <summary>Stock added (found/correction) — audited stock-in at current WAC.</summary>
    Increase = 2,
}
