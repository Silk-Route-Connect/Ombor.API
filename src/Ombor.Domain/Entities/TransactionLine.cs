using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class TransactionLine : EntityBase
{
    /// <summary>
    /// Agreed unit price in the transaction.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Quantity sold / supplied (could be fractional, hence decimal).
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Absolute discount applied to this line (not %).
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Convenience property: (Quantity × UnitPrice) − Discount.
    /// Calculated in code; not mapped unless you make it a computed column.
    /// </summary>
    public decimal LineTotal => (Quantity * UnitPrice) - Discount;

    /// <summary>
    /// Foreign-key to the catalog product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation to the catalog product.
    /// </summary>
    public required virtual Product Product { get; set; }

    /// <summary>
    /// Foreign-key to the parent document.
    /// </summary>
    public int TransactionId { get; set; }

    /// <summary>
    /// Navigation to the parent document.
    /// </summary>
    public required virtual TransactionRecord Transaction { get; set; }
}
