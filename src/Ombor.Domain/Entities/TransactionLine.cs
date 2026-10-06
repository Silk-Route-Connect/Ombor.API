using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class TransactionLine : EntityBase, IOrganizationScoped, IAuditableChild
{
    AuditParent IAuditableChild.AuditParent => new(typeof(TransactionRecord), TransactionId);

    public int OrganizationId { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }

    /// <summary>
    /// How <see cref="Discount"/> is interpreted (rule 37). Defaults to Percentage so a line that
    /// stored its discount before this field existed still computes the same total.
    /// </summary>
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    public decimal Quantity { get; set; }

    /// <summary>
    /// When the line was entered in packages, the package size (base units per package) snapshotted from the
    /// product at event time; null when entered in base units. Audit-only (rule 21): <see cref="Quantity"/>
    /// (base units) is server-computed as <c>pack count × PackageSize</c> and stays the source of truth for
    /// stock and WAC. The entered pack count is recoverable as <c>Quantity / PackageSize</c>, so it is not
    /// stored separately. Snapshotted so a later change to the product's packaging cannot rewrite this record.
    /// </summary>
    public int? PackageSize { get; set; }

    /// <summary>
    /// Line total after discount (rule 37). A <see cref="DiscountType.Percentage"/> discount is
    /// <c>gross × discount / 100</c>; a <see cref="DiscountType.Fixed"/> discount is the value itself.
    /// The discount is clamped to the line gross so a total never goes negative. Kept as a single
    /// inline expression so it stays translatable in EF projections.
    /// </summary>
    public decimal Total =>
        DiscountType == DiscountType.Fixed
            ? (UnitPrice * Quantity) - (Discount > UnitPrice * Quantity ? UnitPrice * Quantity : Discount)
            : (UnitPrice * Quantity) - (UnitPrice * Quantity * (Discount > 100m ? 100m : Discount) / 100m);

    /// <summary>
    /// Cost per base unit at the moment of the event, snapshotted in the same write that moved the stock: a Sale
    /// (incl. a delivered order) and a SupplyRefund store the warehouse WAC the goods left at — for a Sale that is its
    /// COGS (rule 19); a SaleRefund stores the original sale line's cost, at which the goods come back; a Supply
    /// stores its net purchase cost (after the line discount), the cost that entered the WAC. Never re-derived later,
    /// so a past period's profit does not move when prices change. Null on Supply and SupplyRefund lines recorded
    /// before 2026-10-04.
    /// </summary>
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// True when <see cref="UnitCost"/> is an estimate rather than the cost at the moment of the event: Sale and
    /// SaleRefund lines recorded before 2026-10-04 were backfilled from the WAC at the time of the backfill.
    /// Bookkeeping about the cost's origin, not business activity, so the Activity Log leaves it out.
    /// </summary>
    [NotAudited]
    public bool CostIsEstimated { get; set; }

    /// <summary>The line's cost (<see cref="UnitCost"/> × <see cref="Quantity"/>, 2 decimals); null when the cost is unknown.</summary>
    public decimal? Cost => UnitCost is { } unitCost ? Math.Round(unitCost * Quantity, 2, MidpointRounding.AwayFromZero) : null;

    public int ProductId { get; set; }
    public virtual required Product Product { get; set; }

    public int TransactionId { get; set; }
    public virtual required TransactionRecord Transaction { get; set; }
}
