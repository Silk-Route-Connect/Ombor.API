using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class TemplateItem : AuditableEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }

    public int ProductId { get; set; }
    public required virtual Product Product { get; set; }
}
