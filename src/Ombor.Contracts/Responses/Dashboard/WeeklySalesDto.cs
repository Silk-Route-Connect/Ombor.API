namespace Ombor.Contracts.Responses.Reports;

public sealed record WeeklySalesDto(
    int SalesCount,
    decimal Total,
    DateTime DateTime);
