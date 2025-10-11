using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;

namespace Ombor.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto[]> GetAsync(GetOrdersRequest request);
    Task<OrderDto> GetByIdAsync(GetOrderByIdRequest request);
    Task<OrderDto> CreateAsync(CreateOrderRequest request);
}
