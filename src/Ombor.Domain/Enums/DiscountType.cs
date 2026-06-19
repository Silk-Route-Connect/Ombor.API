namespace Ombor.Domain.Enums;

/// <summary>
/// How a line's <c>Discount</c> value is interpreted (business-rules rule 37). Persisted
/// alongside the discount so a fixed amount never rescales when a mutable line is re-priced.
/// </summary>
public enum DiscountType
{
    /// <summary>Discount is a percentage of the line gross (<c>unitPrice × quantity × discount / 100</c>).</summary>
    Percentage = 1,

    /// <summary>Discount is a currency amount taken off the whole line, clamped to the line gross.</summary>
    Fixed = 2,
}
