using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// Enumeration of supported units of measurement for products.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UnitOfMeasurement
{
    /// <summary>The gram unit.</summary>
    Gram = 1,

    /// <summary>The kilogram unit (1000 grams).</summary>
    Kilogram = 2,

    /// <summary>The ton unit (1000 kilograms).</summary>
    Ton = 3,

    /// <summary>A single piece/unit.</summary>
    Piece = 4,

    /// <summary>A box containing an unspecified quantity.</summary>
    Box = 5,

    /// <summary>A generic “unit”.</summary>
    Unit = 6,

    /// <summary>No specific measurement.</summary>
    None = 100,
}