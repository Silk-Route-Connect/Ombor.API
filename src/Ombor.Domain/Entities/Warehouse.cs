using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a warehouse — a physical place that holds stock (its <see cref="WarehouseItems"/>).
/// </summary>
public class Warehouse : AuditableEntity, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>Gets or sets the name of the warehouse.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the location of the warehouse.</summary>
    public string? Location { get; set; }

    /// <summary>Whether the warehouse is archived (hidden from default pickers; still counts in totals, rule 31).</summary>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets the item that belong to this warehouse.</summary>
    public ICollection<WarehouseItem> WarehouseItems { get; set; }

    public Warehouse()
    {
        WarehouseItems = [];
    }
}
