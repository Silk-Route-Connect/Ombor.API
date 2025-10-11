using Ombor.Contracts.Common;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Order;

public sealed record CreateOrderRequest(
    OrderSource Source,
    AddressDto DeliveryAddress,
    IEnumerable<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal? Discount);
