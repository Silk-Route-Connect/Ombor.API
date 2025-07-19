using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class TransactionLine : EntityBase
{
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Quantity { get; set; }

    public int ProductId { get; set; }
    public virtual required Product Product { get; set; }

    public int TransactionId { get; set; }
    public virtual required TransactionRecord Transaction { get; set; }
}
