using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class Order : AuditableEntity
{
    public required string OrderNumber { get; set; }
    public string? Notes { get; set; }
    public required decimal TotalAmount { get; set; }
    public required DateTimeOffset DateUtc { get; set; }
    public OrderStatus Status { get; set; }
    public OrderSource Source { get; set; }

    public required Address DeliveryAddress { get; set; }

    public required int CustomerId { get; set; }
    public required virtual Partner Customer { get; set; }

    public virtual ICollection<OrderLine> Lines { get; set; }

    public Order()
    {
        Lines = [];
    }
}
