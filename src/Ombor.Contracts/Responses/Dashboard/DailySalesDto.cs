namespace Ombor.Contracts.Responses.Dashboard;

public sealed record DailySalesDto(
    int Id,
    string PartnerName,
    decimal TotalDue,
    int SalesCount,
    DateTime Date);
