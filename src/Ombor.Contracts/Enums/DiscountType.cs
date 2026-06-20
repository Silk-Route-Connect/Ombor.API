using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// How a transaction line's discount value is interpreted (business-rules rule 37).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DiscountType
{
    /// <summary>Discount is a percentage of the line gross (<c>unitPrice × quantity × discount / 100</c>).</summary>
    Percentage = 1,

    /// <summary>Discount is a currency amount taken off the whole line, clamped to the line gross.</summary>
    Fixed = 2,
}
