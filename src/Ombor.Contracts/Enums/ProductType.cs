using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// Enumeration of supported product types
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProductType
{
    /// <summary>Product is for sale.</summary>
    Sale = 1,

    /// <summary>Product is for supply.</summary>
    Supply = 2,

    /// <summary>Product is for both sale and supply.</summary>
    SaleAndSupply = 3,
}
