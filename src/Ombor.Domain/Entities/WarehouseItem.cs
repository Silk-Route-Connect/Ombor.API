using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a single item of a <see cref="Warehouse"/>.
/// </summary>
public class WarehouseItem : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the weighted-average unit cost, updated atomically on every stock-in event.
    /// </summary>
    public decimal AverageCost { get; set; }

    /// <summary>
    /// Gets or sets quantity of the <see cref="WarehouseItem"/>.
    /// </summary>
    public required int Quantity { get; set; }

    /// <summary>
    /// Gets or sets product ID of the <see cref="WarehouseItem"/>.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets product of the <see cref="WarehouseItem"/>.
    /// </summary>
    public required virtual Product Product { get; set; }

    /// <summary>
    /// Gets or sets warehouse ID of the <see cref="Warehouse"/>.
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets warehouse of the <see cref="Warehouse"/>.
    /// </summary>
    public required virtual Warehouse Warehouse { get; set; }
}
