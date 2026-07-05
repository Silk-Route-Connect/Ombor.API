using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Order;

/// <summary>Creates an order in <c>Pending</c> status. No stock is reserved.</summary>
/// <param name="CustomerId">The customer the order is for (required).</param>
/// <param name="Source">Where the order originated.</param>
/// <param name="WarehouseId">The intended warehouse. Non-binding — reserves no stock.</param>
/// <param name="DeliveryAddress">Optional free-text delivery address.</param>
/// <param name="DeliveryDate">Optional delivery date.</param>
/// <param name="DeliveryTime">Optional delivery time of day.</param>
/// <param name="Notes">Optional free-text note.</param>
/// <param name="Lines">The order lines (at least one).</param>
public sealed record CreateOrderRequest(
    int CustomerId,
    OrderSource Source,
    int? WarehouseId,
    string? DeliveryAddress,
    DateOnly? DeliveryDate,
    TimeOnly? DeliveryTime,
    string? Notes,
    IEnumerable<CreateOrderLineRequest> Lines);

/// <summary>A single line of an order.</summary>
/// <param name="ProductId">The product ordered.</param>
/// <param name="Quantity">Quantity.</param>
/// <param name="UnitPrice">Price per unit.</param>
/// <param name="Discount">Discount value, interpreted per <paramref name="DiscountType"/> (rule 37).</param>
/// <param name="DiscountType">Whether <paramref name="Discount"/> is a percentage or a fixed amount.</param>
public sealed record CreateOrderLineRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal? Discount,
    DiscountType DiscountType);
