using Ombor.Contracts.Common;
using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class OrderMappings
{
    private static readonly TimeZoneInfo TashkentTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");

    public static Order ToEntity(this CreateOrderRequest request)
    {
        var lines = request.Lines.ToEntity();
        var totalAmount = lines.Sum(line => line.TotalPrice);

        return new Order
        {
            CustomerId = request.CustomerId,
            DateUtc = DateTime.UtcNow,
            DeliveryAddress = request.DeliveryAddress.ToEntity(),
            OrderNumber = Guid.NewGuid().ToString("N").ToUpperInvariant()[..10],
            TotalAmount = totalAmount,
            Lines = lines,
            Status = Domain.Enums.OrderStatus.Pending,
            Source = Enum.Parse<Domain.Enums.OrderSource>(request.Source.ToString(), ignoreCase: true),
            Notes = request.Notes,
            Customer = null!, // Will be set by EF
        };
    }

    public static OrderDto ToDto(this Order order)
    {
        if (order.Customer is null)
        {
            throw new InvalidOperationException("Cannot map Order to OrderDto because Customer is null.");
        }

        return new(
            Id: order.Id,
            CustomerId: order.CustomerId,
            CustomerName: order.Customer.Name,
            OrderNumber: order.OrderNumber,
            Notes: order.Notes,
            TotalAmount: order.TotalAmount,
            Date: TimeZoneInfo.ConvertTimeFromUtc(order.DateUtc.DateTime, TashkentTimeZone),
            Status: order.Status.ToString(),
            Source: order.Source.ToString(),
            DeliveryAddress: order.DeliveryAddress.ToDto(),
            Lines: order.Lines.ToDto());
    }

    public static OrderLineDto[] ToDto(this IEnumerable<OrderLine> lines)
        => [.. lines.Select(ToDto)];

    public static OrderLineDto ToDto(this OrderLine line)
    {
        if (line.Product is null)
        {
            throw new InvalidOperationException("Cannot map OrderLine to OrderLineDto because Product is null.");
        }

        return new(
            line.Id,
            line.ProductId,
            line.Product.Name,
            line.Quantity,
            line.UnitPrice,
            line.Discount);
    }

    private static OrderLine[] ToEntity(this IEnumerable<CreateOrderLineRequest> dtos)
        => [.. dtos.Select(ToEntity)];

    private static OrderLine ToEntity(this CreateOrderLineRequest dto)
        => new()
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            Discount = dto.Discount,
            Product = null!, // Will be set by EF
            Order = null!,   // Will be set by EF
        };

    private static Address ToEntity(this AddressDto dto)
        => new()
        {
            Latitude = dto.Latitude,
            Longtitude = dto.Longtitude
        };

    private static AddressDto ToDto(this Address address)
        => new(
            Latitude: address.Latitude,
            Longtitude: address.Longtitude);
}
