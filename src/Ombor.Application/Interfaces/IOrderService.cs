using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;

namespace Ombor.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto[]> GetAsync(GetOrdersRequest request);
    Task<OrderDto> GetByIdAsync(GetOrderByIdRequest request);
    Task<OrderDto> CreateAsync(CreateOrderRequest request);
    Task<OrderDto> UpdateAsync(UpdateOrderRequest request);
    Task<OrderDto> ProcessAsync(ProcessOrderRequest request);
    Task<OrderDto> ShipAsync(ShipOrderRequest request);
    Task<OrderDto> CancelAsync(CancelOrderRequest request);
    Task<OrderDto> ReturnAsync(ReturnOrderRequest request);
    Task<OrderDto> RejectAsync(RejectOrderRequest request);
    Task<OrderDto> DeliverAsync(DeliverOrderRequest request);
}
