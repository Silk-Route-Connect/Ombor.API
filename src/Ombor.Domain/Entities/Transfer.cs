using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// An immutable inter-warehouse stock transfer: decrements the source warehouse and increments the
/// destination (carrying the source's weighted-average cost) as one atomic operation.
/// </summary>
public class Transfer : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    public DateTimeOffset DateUtc { get; set; }
    public string? Notes { get; set; }

    /// <summary>The user who made the transfer; null outside an authenticated request.</summary>
    public string? CreatedBy { get; set; }

    public int FromWarehouseId { get; set; }
    public virtual required Warehouse FromWarehouse { get; set; }

    public int ToWarehouseId { get; set; }
    public virtual required Warehouse ToWarehouse { get; set; }

    public virtual ICollection<TransferLine> Lines { get; set; } = [];
}
