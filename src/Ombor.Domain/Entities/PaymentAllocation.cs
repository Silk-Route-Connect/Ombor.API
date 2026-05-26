using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class PaymentAllocation : EntityBase, ITenantScoped, IAuditable
{
    public int TenantId { get; set; }

    public decimal Amount { get; set; }
    public PaymentAllocationType Type { get; set; }

    public int? TransactionId { get; set; }
    public TransactionRecord? Transaction { get; set; }

    public int PaymentId { get; set; }
    public virtual required Payment Payment { get; set; }
}
