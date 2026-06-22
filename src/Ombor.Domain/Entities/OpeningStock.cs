using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// An immutable opening-stock event: the initial quantity and unit cost stocked for one product in one
/// warehouse. The opening cost sets the product's initial weighted-average cost in that warehouse.
/// </summary>
public class OpeningStock : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    public DateTimeOffset DateUtc { get; set; }

    /// <summary>The opening quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>The opening unit cost — sets the initial weighted-average cost.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>The user who recorded the opening stock; null for seed/system rows.</summary>
    public int? CreatedById { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public int WarehouseId { get; set; }
    public required virtual Warehouse Warehouse { get; set; }

    public int ProductId { get; set; }
    public required virtual Product Product { get; set; }
}
