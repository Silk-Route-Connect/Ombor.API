namespace Ombor.Contracts.Enums;

/// <summary>
/// Distinguishes how each slice of a payment is applied.
/// </summary>
public enum PaymentAllocationType
{
    /// <summary>
    /// Portion applied to an open <see cref="TransactionType.Sale"/>.
    /// </summary>
    Sale = 1,

    /// <summary>
    /// Portion applied to an open <see cref="TransactionType.Supply"/>.
    /// </summary>
    Supply = 2,

    /// <summary>
    /// Portion applied to a customer-side refund of a previous sale.
    /// </summary>
    SaleRefund = 3,

    /// <summary>
    /// Portion applied to a supplier-side refund of a previous supply.
    /// </summary>
    SupplyRefund = 4,

    /// <summary>
    /// Portion kept on the partner’s balance for future use (deposit / over-payment).
    /// </summary>
    AdvancePayment = 5
}
