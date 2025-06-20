namespace Ombor.Contracts.Enums;

/// <summary>
/// Commercial transaction categories stored in the system.
/// </summary>
public enum TransactionType
{
    /// <summary>Sale of goods or services to a customer.</summary>
    Sale = 1,

    /// <summary>Purchase of goods or services from a supplier.</summary>
    Supply = 2,

    /// <summary>Customer-side refund for a previous sale.</summary>
    SaleRefund = 3,

    /// <summary>Supplier-side refund for a previous supply.</summary>
    SupplyRefund = 4
}
