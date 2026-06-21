namespace Ombor.Domain.Common;

/// <summary>
/// A delivery address: free-text plus optional coordinates. Every member is optional — the coordinates
/// are dormant today (reserved for a future geo-delivery feature) and stay null until then. The owning
/// entity always holds a non-null <see cref="Address"/>; absence is expressed by null members.
/// </summary>
public sealed class Address
{
    /// <summary>The free-text address.</summary>
    public string? Text { get; set; }

    /// <summary>Latitude; null when unknown.</summary>
    public decimal? Latitude { get; set; }

    /// <summary>Longitude; null when unknown.</summary>
    public decimal? Longitude { get; set; }
}
