using Ombor.Contracts.Abstractions;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Order;

/// <summary>
/// Confirms delivery of an order, promoting it to a real Sale at <paramref name="WarehouseId"/>.
/// </summary>
/// <param name="OrderId">The order being delivered.</param>
/// <param name="WarehouseId">The warehouse the stock is drawn from (authoritative — hard stock check here).</param>
public sealed record DeliverOrderRequest(int OrderId, int WarehouseId) : IOrderStateUpdateRequest
{
    public OrderStatus TargetStatus => OrderStatus.Delivered;
}

/// <summary>The body of <c>POST /api/orders/{id}/deliver</c> — the order id comes from the route.</summary>
/// <param name="WarehouseId">The warehouse the stock is drawn from.</param>
public sealed record DeliverOrderBody(int WarehouseId);
