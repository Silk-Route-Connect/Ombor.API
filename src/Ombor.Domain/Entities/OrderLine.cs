using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class OrderLine : AuditableEntity
{
    public required int Quantity { get; set; }
    public required decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }

    public decimal TotalPrice => (UnitPrice * Quantity) - (Discount ?? 0);

    public int OrderId { get; set; }
    public required virtual Order Order { get; set; }

    public int ProductId { get; set; }
    public required virtual Product Product { get; set; }
}
