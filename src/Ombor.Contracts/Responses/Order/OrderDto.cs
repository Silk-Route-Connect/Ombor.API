using Ombor.Contracts.Common;

namespace Ombor.Contracts.Responses.Order;

public sealed record OrderDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string OrderNumber,
    string? Notes,
    decimal TotalAmount,
    DateTime Date,
    string Status,
    string Source,
    AddressDto DeliveryAddress,
    OrderLineDto[] Lines);

public sealed record OrderLineDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal? Discount);
