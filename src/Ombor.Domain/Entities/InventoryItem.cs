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
    /// Gets or sets product ID of the <see cref="InventoryItem"/>.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets product of the <see cref="InventoryItem"/>.
    /// </summary>
    public required virtual Product Product { get; set; }

    /// <summary>
    /// Gets or sets inventory ID of the <see cref="Inventory"/>.
    /// </summary>
    public int InventoryId { get; set; }

    /// <summary>
    /// Gets or sets inventory of the <see cref="Inventory"/>.
    /// </summary>
    public required virtual Inventory Inventory { get; set; }
}
