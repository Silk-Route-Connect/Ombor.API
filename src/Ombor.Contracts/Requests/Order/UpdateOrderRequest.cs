using Ombor.Contracts.Common;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Order;

/// <summary>
/// Edits an order while it is still open (before shipping). Lines and scalar fields are replaced
/// wholesale; <see cref="WarehouseId"/> is tri-state: absent = keep, <c>null</c> = clear, value = set.
/// </summary>
/// <param name="Id">The order being edited.</param>
/// <param name="CustomerId">The customer the order is for.</param>
/// <param name="Source">Where the order originated.</param>
/// <param name="WarehouseId">The intended warehouse (tri-state: absent = keep, null = clear, value = set).</param>
/// <param name="DeliveryAddress">Optional free-text delivery address.</param>
/// <param name="DeliveryDate">Optional delivery date.</param>
/// <param name="DeliveryTime">Optional delivery time of day.</param>
/// <param name="Notes">Optional free-text note.</param>
/// <param name="Lines">The replacement order lines (at least one).</param>
public sealed record UpdateOrderRequest(
    int Id,
    int CustomerId,
    OrderSource Source,
    Optional<int?> WarehouseId,
    string? DeliveryAddress,
    DateOnly? DeliveryDate,
    TimeOnly? DeliveryTime,
    string? Notes,
    IEnumerable<CreateOrderLineRequest> Lines);
