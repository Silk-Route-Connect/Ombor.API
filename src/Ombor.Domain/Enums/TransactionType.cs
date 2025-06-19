namespace Ombor.Domain.Enums;

/// <summary>
/// Enumeration of all commercial transaction types.
/// </summary>
public enum TransactionType
{
    /// <summary>No type set – should not be persisted.</summary>
    None = 0,

    /// <summary>Product/service sale to a customer.</summary>
    Sale = 1,

    /// <summary>Product/service purchase from a supplier.</summary>
    Supply = 2,

    /// <summary>Refund (return) issued to a customer for a previous sale.</summary>
    SaleRefund = 3,

    /// <summary>Refund (return) received from a supplier for a previous supply.</summary>
    SupplyRefund = 4,
}
