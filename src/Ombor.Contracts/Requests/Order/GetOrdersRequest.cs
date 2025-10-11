namespace Ombor.Contracts.Requests.Order;

public sealed record GetOrdersRequest(
    string? SearchTerm,
    int? CustomerId,
    DateTime? FromDate,
    DateTime? ToDate);
