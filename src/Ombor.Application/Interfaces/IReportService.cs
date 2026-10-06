using Ombor.Contracts.Requests.Report;
using Ombor.Contracts.Responses.Report;

namespace Ombor.Application.Interfaces;

/// <summary>
/// The «Отчёты» read models: server-computed aggregates over the event ledger for one organization, on the business
/// (Tashkent) calendar. Nothing here is stored; every figure is derived per request from the immutable events.
/// </summary>
public interface IReportService
{
    /// <summary>Sales net of refunds with cost and gross profit.</summary>
    Task<SalesReportDto> GetSalesAsync(GetSalesReportRequest request);

    /// <summary>Supplies net of supply refunds.</summary>
    Task<PurchasesReportDto> GetPurchasesAsync(GetPurchasesReportRequest request);

    /// <summary>Stock on hand now, valued at cost and at sale price, with low-stock flags.</summary>
    Task<StockReportDto> GetStockAsync(GetStockReportRequest request);

    /// <summary>Wallet balances at the start and end of a period and the money that moved in between.</summary>
    Task<CashFlowReportDto> GetCashFlowAsync(GetCashFlowReportRequest request);

    /// <summary>Money paid out by type, partner, employee and description.</summary>
    Task<ExpensesReportDto> GetExpensesAsync(GetReportPeriodRequest request);

    /// <summary>Stock written off by decrease adjustments.</summary>
    Task<LossesReportDto> GetLossesAsync(GetReportPeriodRequest request);

    /// <summary>A simple profit and loss.</summary>
    Task<ProfitReportDto> GetProfitAsync(GetProfitReportRequest request);
}
