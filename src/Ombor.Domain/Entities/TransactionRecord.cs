using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class TransactionRecord : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public DateTimeOffset DateUtc { get; set; }
    public DateOnly? DueDate { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }

    public decimal UnpaidAmount => TotalDue - TotalPaid;

    /// <summary>Optional free-text note captured when the transaction was created.</summary>
    public string? Notes { get; set; }

    /// <summary>The user who created the transaction; null for seed/system rows. Resolved to a display name on read.</summary>
    public int? CreatedById { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public int PartnerId { get; set; }
    public virtual required Partner Partner { get; set; }

    /// <summary>Warehouse whose stock this transaction affects. Required for stock-affecting types.</summary>
    public int? WarehouseId { get; set; }
    public virtual Warehouse? Warehouse { get; set; }

    /// <summary>The original transaction this one reverses. Required for SaleRefund and SupplyRefund.</summary>
    public int? OriginalTransactionId { get; set; }
    public virtual TransactionRecord? OriginalTransaction { get; set; }

    /// <summary>Why the refund was issued. Set only on SaleRefund and SupplyRefund transactions.</summary>
    public string? RefundReason { get; set; }

    public virtual ICollection<TransactionLine> Lines { get; set; } = [];

    public virtual ICollection<TransactionAttachment> Attachments { get; set; } = [];

    public virtual ICollection<PaymentAllocation> PaymentAllocations { get; set; } = [];
}
