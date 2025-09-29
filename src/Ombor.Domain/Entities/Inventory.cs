using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents an Inventory entity.
/// </summary>
public class Inventory : AuditableEntity
{
    /// <summary>Gets or sets the name of the inventory.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the location of the inventory.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the status of thr inventory.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the item that belong to this inventory.</summary>
    public ICollection<InventoryItem> InventoryItems { get; set; }

    public Inventory()
    {
        InventoryItems = [];
    }
}
