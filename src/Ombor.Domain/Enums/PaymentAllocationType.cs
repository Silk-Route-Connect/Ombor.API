namespace Ombor.Domain.Enums;

/// <summary>
/// The destination side of a payment (rule 10). <see cref="TransactionSettlement"/> and
/// <see cref="AdvanceCredit"/> are settling allocations inside the rule-8 balance identity;
/// <see cref="ChangeReturn"/> is a stored audit memo, excluded from it.
/// </summary>
public enum PaymentAllocationType
{
    /// <summary>Settles a specific transaction's debt.</summary>
    TransactionSettlement = 7,

    /// <summary>Parks money as a partner's advance claim. Permitted only when the partner has no outstanding debt (rule 40).</summary>
    AdvanceCredit = 8,

    /// <summary>Cash handed straight back to the partner; recorded for audit only and excluded from every balance (rule 10).</summary>
    ChangeReturn = 6,

    // Carried for backward compatibility with existing payment records; the values above are the source of truth.
    Sale = 1,
    Supply = 2,
    SaleRefund = 3,
    SupplyRefund = 4,
    AdvancePayment = 5,
}
