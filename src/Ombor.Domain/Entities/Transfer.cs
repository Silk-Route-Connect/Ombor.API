using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// An inter-warehouse stock transfer: decrements the source inventory and increments
/// the destination inventory as one tracked operation.
/// </summary>
public class Transfer : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    public DateTimeOffset DateUtc { get; set; }
    public TransferStatus Status { get; set; }
    public string? Notes { get; set; }

    public int FromInventoryId { get; set; }
    public virtual required Inventory FromInventory { get; set; }

    public int ToInventoryId { get; set; }
    public virtual required Inventory ToInventory { get; set; }

    public virtual ICollection<TransferLine> Lines { get; set; } = [];
}
