using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// One row per "slice" of a payment that is applied to a specific transaction or held as advance credit.
/// </summary>
public class PaymentAllocation : EntityBase
{
    /// <summary>
    /// Portion of <see cref="Payment.Amount"/> that this row represents.
    /// </summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>
    /// Nature of the allocation (Sale settlement, AdvancePayment, …).
    /// </summary>
    public PaymentAllocationType Type { get; set; }

    /// <summary>
    /// FK to the transaction being settled.
    /// <c>null</c> when <see cref="Type"/> = <see cref="PaymentAllocationType.AdvancePayment"/>.
    /// </summary>
    public int? TransactionId { get; set; }

    /// <summary>
    /// Navigation to the transaction being settled.
    /// </summary>
    public TransactionRecord? Transaction { get; set; }

    /// <summary>
    /// FK to the parent payment.
    /// </summary>
    public int PaymentId { get; set; }

    /// <summary>
    /// Navigation to the parent payment.
    /// </summary>
    public required Payment Payment { get; set; }
}
