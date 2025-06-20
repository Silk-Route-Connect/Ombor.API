namespace Ombor.Contracts.Enums;

/// <summary>
/// Distinguishes whether a refund is made against a customer sale or a supplier purchase.
/// </summary>
public enum TransactionRefundType
{
    /// <summary>
    /// Refund is made against a Sale.
    /// </summary>
    Sale = 1,

    /// <summary>
    /// Refund is made against a Supply.
    /// </summary>
    Supply = 2,
}
