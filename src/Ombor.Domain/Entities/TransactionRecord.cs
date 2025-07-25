using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class TransactionRecord : EntityBase
{
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public DateTimeOffset DateUtc { get; set; }
    public DateOnly? DueDate { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }

    public decimal UnpaidAmount => TotalDue - TotalPaid;

    public int PartnerId { get; set; }
    public virtual required Partner Partner { get; set; }

    public virtual ICollection<TransactionLine> Lines { get; set; } = [];

    public virtual ICollection<PaymentAllocation> PaymentAllocations { get; set; } = [];
}
