using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// An inter-warehouse stock transfer: decrements the source warehouse and increments
/// the destination warehouse as one tracked operation.
/// </summary>
public class Transfer : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    public DateTimeOffset DateUtc { get; set; }
    public TransferStatus Status { get; set; }
    public string? Notes { get; set; }

    public int FromWarehouseId { get; set; }
    public virtual required Warehouse FromWarehouse { get; set; }

    public int ToWarehouseId { get; set; }
    public virtual required Warehouse ToWarehouse { get; set; }

    public virtual ICollection<TransferLine> Lines { get; set; } = [];
}
