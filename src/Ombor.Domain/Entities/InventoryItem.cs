using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a single item of a <see cref="Inventory"/>.
/// </summary>
public class InventoryItem : EntityBase
{
    /// <summary>
    /// Gets or sets quantity of the <see cref="InventoryItem"/>.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets location of the <see cref="InventoryItem"/>.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets Product ID of the <see cref="InventoryItem"/>.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets Product of the <see cref="InventoryItem"/>.
    /// </summary>
    public required virtual Product Product { get; set; }
}
