namespace Ombor.Domain.Enums;

/// <summary>
/// The destination side of a payment (rule 10). <see cref="TransactionSettlement"/> and
/// <see cref="AdvanceCredit"/> are settling allocations inside the rule-8 identity;
/// <see cref="ChangeReturn"/> is a stored audit memo, excluded from it.
/// </summary>
public enum PaymentAllocationType
{
    // --- Legacy values. Removed in the M2 subtractive migration once no consumer references them. ---
    Sale = 1,

    Supply = 2,

    SaleRefund = 3,

    SupplyRefund = 4,

    AdvancePayment = 5,

    // --- Redesigned values (rule 10). ChangeReturn is shared with the legacy model. ---
    ChangeReturn = 6,

    /// <summary>Settles a specific transaction's debt.</summary>
    TransactionSettlement = 7,

    /// <summary>Parks money as a partner's advance claim (gated on zero outstanding debt — rule 40).</summary>
    AdvanceCredit = 8,
}
