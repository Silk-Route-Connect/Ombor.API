using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;

namespace Ombor.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto[]> GetAsync(GetOrdersRequest request);
    Task<OrderDto> GetByIdAsync(GetOrderByIdRequest request);
    Task<OrderDto> CreateAsync(CreateOrderRequest request);
    Task ProcessAsync(ProcessOrderRequest request);
    Task ShipAsync(ShipOrderRequest request);
    Task CancelAsync(CancelOrderRequest request);
    Task ReturnAsync(ReturnOrderRequest request);
    Task RejectAsync(RejectOrderRequest request);
    Task DeliverAsync(DeliverOrderRequest request);
}
