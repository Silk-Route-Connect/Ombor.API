namespace Ombor.Contracts.Requests.Order;

public sealed record GetOrdersRequest(
    string? SearchTerm,
    int? PartnerId,
    DateTime? FromDate,
    DateTime? ToDate);
