using Ombor.Application.Extensions;
using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class OrderMappings
{
    private static readonly TimeZoneInfo TashkentTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Central Asia Standard Time" : "Asia/Tashkent");

    public static Order ToEntity(this CreateOrderRequest request)
    {
        var lines = request.Lines.ToEntity();
        var totalAmount = lines.Sum(line => line.TotalPrice);

        return new Order
        {
            CustomerId = request.CustomerId,
            DateUtc = DateTime.UtcNow,
            OrderNumber = Guid.NewGuid().ToString("N").ToUpperInvariant()[..10],
            TotalAmount = totalAmount,
            Lines = lines,
            Status = Domain.Enums.OrderStatus.Pending,
            Source = Enum.Parse<Domain.Enums.OrderSource>(request.Source.ToString(), ignoreCase: true),
            Notes = request.Notes,
            WarehouseId = request.WarehouseId,
            DeliveryAddress = request.DeliveryAddress,
            DeliveryDate = request.DeliveryDate,
            DeliveryTime = request.DeliveryTime,
            Customer = null!, // Will be set by EF
        };
    }

    public static OrderLine[] ToEntity(this IEnumerable<CreateOrderLineRequest> dtos)
        => [.. dtos.Select(ToEntity)];

    public static OrderDto ToDto(this Order order, decimal customerBalance, IReadOnlyDictionary<int, string> actorNames)
    {
        if (order.Customer is null)
        {
            throw new InvalidOperationException("Cannot map Order to OrderDto because Customer is null.");
        }

        return new(
            Id: order.Id,
            CustomerId: order.CustomerId,
            CustomerName: order.Customer.Name,
            CustomerType: order.Customer.Type.ToString(),
            CustomerBalance: customerBalance,
            OrderNumber: order.OrderNumber,
            Notes: order.Notes,
            Total: order.TotalAmount,
            Date: TimeZoneInfo.ConvertTimeFromUtc(order.DateUtc.UtcDateTime, TashkentTimeZone),
            Status: order.Status.ToString(),
            Source: order.Source.ToString(),
            DeliveryAddress: order.DeliveryAddress,
            DeliveryDate: order.DeliveryDate,
            DeliveryTime: order.DeliveryTime,
            WarehouseId: order.WarehouseId,
            WarehouseName: order.Warehouse?.Name,
            SaleId: order.SaleId,
            Lines: order.Lines.ToDto(),
            History: order.History.ToDto(actorNames));
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
            line.Product.SKU,
            line.Product.Measurement.ToString(),
            line.Quantity,
            line.UnitPrice,
            line.Discount,
            line.DiscountType.ToString(),
            line.TotalPrice);
    }

    private static OrderStatusEventDto[] ToDto(this IEnumerable<OrderStatusEvent> history, IReadOnlyDictionary<int, string> actorNames)
        => [.. history
            .OrderBy(e => e.At)
            .ThenBy(e => e.Id)
            .Select(e => new OrderStatusEventDto(
                e.At,
                e.From?.ToString(),
                e.To.ToString(),
                e.By is int by && actorNames.TryGetValue(by, out var name) ? name : null))];

    private static OrderLine ToEntity(this CreateOrderLineRequest dto)
        => new()
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            Discount = dto.Discount,
            DiscountType = dto.DiscountType.ToDomainDiscountType(),
            Product = null!, // Will be set by EF
            Order = null!,   // Will be set by EF
        };
}
