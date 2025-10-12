using Ombor.Contracts.Abstractions;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Order;

public sealed record DeliverOrderRequest(int OrderId) : IOrderStateUpdateRequest
{
    public OrderStatus TargetStatus => OrderStatus.Delivered;
}
