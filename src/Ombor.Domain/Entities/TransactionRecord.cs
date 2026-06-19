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

    public int PartnerId { get; set; }
    public virtual required Partner Partner { get; set; }

    /// <summary>Warehouse whose stock this transaction affects. Required for stock-affecting types.</summary>
    public int? InventoryId { get; set; }
    public virtual Inventory? Inventory { get; set; }

    /// <summary>The original transaction this one reverses. Required for SaleRefund and SupplyRefund.</summary>
    public int? OriginalTransactionId { get; set; }
    public virtual TransactionRecord? OriginalTransaction { get; set; }

    public virtual ICollection<TransactionLine> Lines { get; set; } = [];

    public virtual ICollection<PaymentAllocation> PaymentAllocations { get; set; } = [];
}
