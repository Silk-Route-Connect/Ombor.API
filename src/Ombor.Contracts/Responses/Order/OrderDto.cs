namespace Ombor.Contracts.Responses.Order;

/// <summary>An order with its computed totals, customer balance, lines, and status history.</summary>
/// <param name="Id">The order id.</param>
/// <param name="CustomerId">The customer the order is for.</param>
/// <param name="CustomerName">The customer's display name.</param>
/// <param name="CustomerType">The customer's partner type.</param>
/// <param name="CustomerBalance">The customer's net balance (positive = owes us).</param>
/// <param name="OrderNumber">The human-friendly order number.</param>
/// <param name="Notes">Optional free-text note.</param>
/// <param name="Total">The order total (sum of line totals).</param>
/// <param name="Date">The order date, in the organization's local time.</param>
/// <param name="Status">The current order status.</param>
/// <param name="Source">Where the order originated.</param>
/// <param name="DeliveryAddress">Optional free-text delivery address.</param>
/// <param name="DeliveryDate">Optional delivery date.</param>
/// <param name="DeliveryTime">Optional delivery time of day.</param>
/// <param name="WarehouseId">The intended warehouse id, if any.</param>
/// <param name="WarehouseName">The intended warehouse name, if any.</param>
/// <param name="SaleId">The Sale this order was promoted into on delivery; null until delivered.</param>
/// <param name="Lines">The order lines.</param>
/// <param name="History">The status-transition history, oldest-first.</param>
public sealed record OrderDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string CustomerType,
    decimal CustomerBalance,
    string OrderNumber,
    string? Notes,
    decimal Total,
    DateTime Date,
    string Status,
    string Source,
    string? DeliveryAddress,
    DateOnly? DeliveryDate,
    TimeOnly? DeliveryTime,
    int? WarehouseId,
    string? WarehouseName,
    int? SaleId,
    OrderLineDto[] Lines,
    OrderStatusEventDto[] History);

/// <summary>A single order line with its computed total.</summary>
/// <param name="Id">The line id.</param>
/// <param name="ProductId">The product ordered.</param>
/// <param name="ProductName">The product's display name.</param>
/// <param name="Sku">The product SKU.</param>
/// <param name="Measurement">The product's unit of measurement.</param>
/// <param name="Quantity">Quantity.</param>
/// <param name="UnitPrice">Price per unit.</param>
/// <param name="Discount">The raw discount value.</param>
/// <param name="DiscountType">How the discount is interpreted (Percentage or Fixed).</param>
/// <param name="Total">The line total after discount (rule 37).</param>
public sealed record OrderLineDto(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    string Measurement,
    decimal Quantity,
    decimal UnitPrice,
    decimal? Discount,
    string DiscountType,
    decimal Total);

/// <summary>One status transition in an order's history.</summary>
/// <param name="At">When the transition happened.</param>
/// <param name="From">The status before the transition; null for the creation event.</param>
/// <param name="To">The status after the transition.</param>
/// <param name="By">The user who made the transition, if known.</param>
public sealed record OrderStatusEventDto(
    DateTimeOffset At,
    string? From,
    string To,
    string? By);
