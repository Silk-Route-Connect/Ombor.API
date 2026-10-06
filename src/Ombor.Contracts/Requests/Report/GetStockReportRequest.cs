namespace Ombor.Contracts.Requests.Report;

/// <summary>The stock report: what is on hand now and what it is worth.</summary>
/// <param name="WarehouseId">Only this warehouse; default every warehouse, archived ones included (rule 31).</param>
public sealed record GetStockReportRequest(int? WarehouseId = null);
