using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class Order : AuditableEntity, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    public required string OrderNumber { get; set; }
    public string? Notes { get; set; }
    public required decimal TotalAmount { get; set; }
    public required DateTimeOffset DateUtc { get; set; }
    public OrderStatus Status { get; set; }
    public OrderSource Source { get; set; }

    /// <summary>
    /// The delivery address — free-text plus dormant, optional coordinates (reserved for future geo
    /// delivery). A required complex property: always present, with individually optional members.
    /// </summary>
    public Address DeliveryAddress { get; set; } = new();

    /// <summary>Optional delivery date.</summary>
    public DateOnly? DeliveryDate { get; set; }

    /// <summary>Optional delivery time of day.</summary>
    public TimeOnly? DeliveryTime { get; set; }

    /// <summary>
    /// The warehouse the order is intended to ship from. Captured at creation and editable while open,
    /// but non-binding — it reserves no stock. The authoritative warehouse is chosen at delivery.
    /// </summary>
    public int? WarehouseId { get; set; }
    public virtual Inventory? Warehouse { get; set; }

    /// <summary>
    /// The Sale this order was promoted into on delivery. Null until delivered.
    /// </summary>
    public int? SaleId { get; set; }
    public virtual TransactionRecord? Sale { get; set; }

    public required int CustomerId { get; set; }
    public required virtual Partner Customer { get; set; }

    public virtual ICollection<OrderLine> Lines { get; set; }

    /// <summary>The status-transition history, one event per transition (and one at creation).</summary>
    public virtual ICollection<OrderStatusEvent> History { get; set; }

    public Order()
    {
        Lines = [];
        History = [];
    }
}
