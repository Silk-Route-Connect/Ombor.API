using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// An immutable stock-adjustment event (loss or correction) for one product in one warehouse.
/// Increase is an audited stock-in at the current carrying cost; Decrease removes stock and snapshots
/// the WAC as the loss cost. Corrections are new counter-events — there is no edit or delete.
/// </summary>
public class StockAdjustment : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    public DateTimeOffset DateUtc { get; set; }

    public StockAdjustmentDirection Direction { get; set; }

    /// <summary>The adjusted quantity as a positive magnitude.</summary>
    public int Quantity { get; set; }

    /// <summary>Why the adjustment was made (validated against the per-direction reason set).</summary>
    public required string Reason { get; set; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; set; }

    /// <summary>The weighted-average unit cost at the time of the adjustment — the loss cost on a Decrease.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>The user who made the adjustment; null outside an authenticated request.</summary>
    public string? CreatedBy { get; set; }

    public int WarehouseId { get; set; }
    public required virtual Warehouse Warehouse { get; set; }

    public int ProductId { get; set; }
    public required virtual Product Product { get; set; }
}
