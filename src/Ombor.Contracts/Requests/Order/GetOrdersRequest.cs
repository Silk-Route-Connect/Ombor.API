using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Order;

/// <summary>Filters for the orders list.</summary>
/// <param name="SearchTerm">Optional term matched against order number, notes, and customer name.</param>
/// <param name="Status">Optional status to filter by.</param>
/// <param name="CustomerId">Optional customer to filter by.</param>
/// <param name="FromDate">Optional inclusive lower bound on the order date.</param>
/// <param name="ToDate">Optional inclusive upper bound on the order date.</param>
public sealed record GetOrdersRequest(
    string? SearchTerm,
    OrderStatus? Status,
    int? CustomerId,
    DateTime? FromDate,
    DateTime? ToDate);
