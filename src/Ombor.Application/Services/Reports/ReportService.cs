using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Report;
using Ombor.Contracts.Responses.Report;

namespace Ombor.Application.Services.Reports;

/// <summary>
/// Validates a report request, resolves its period on the business calendar and hands it to the report's builder. A
/// warehouse or wallet filter must be one of the caller's own (400 otherwise), like an id in a write body.
/// </summary>
internal sealed class ReportService(
    IApplicationDbContext context,
    IRequestValidator validator,
    IBusinessClock clock,
    SalesReportBuilder sales,
    PurchasesReportBuilder purchases,
    StockReportBuilder stock,
    CashFlowReportBuilder cashFlow,
    ExpensesReportBuilder expenses,
    LossesReportBuilder losses,
    ProfitReportBuilder profit) : IReportService
{
    public async Task<SalesReportDto> GetSalesAsync(GetSalesReportRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await sales.BuildAsync(ReportRange.Resolve(request.From, request.To, clock), request.GroupBy ?? ReportGroupBy.Day);
    }

    public async Task<PurchasesReportDto> GetPurchasesAsync(GetPurchasesReportRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await purchases.BuildAsync(ReportRange.Resolve(request.From, request.To, clock), request.GroupBy ?? ReportGroupBy.Day);
    }

    public async Task<StockReportDto> GetStockAsync(GetStockReportRequest request)
    {
        await validator.ValidateAndThrowAsync(request);
        await OwnedReferences.Check()
            .Require(context.Warehouses, request.WarehouseId, nameof(request.WarehouseId))
            .ThrowIfMissingAsync();

        return await stock.BuildAsync(request.WarehouseId);
    }

    public async Task<CashFlowReportDto> GetCashFlowAsync(GetCashFlowReportRequest request)
    {
        await validator.ValidateAndThrowAsync(request);
        await OwnedReferences.Check()
            .Require(context.Wallets, request.WalletId, nameof(request.WalletId))
            .ThrowIfMissingAsync();

        return await cashFlow.BuildAsync(ReportRange.Resolve(request.From, request.To, clock), request.WalletId);
    }

    public async Task<ExpensesReportDto> GetExpensesAsync(GetReportPeriodRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await expenses.BuildAsync(ReportRange.Resolve(request.From, request.To, clock));
    }

    public async Task<LossesReportDto> GetLossesAsync(GetReportPeriodRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await losses.BuildAsync(ReportRange.Resolve(request.From, request.To, clock));
    }

    public async Task<ProfitReportDto> GetProfitAsync(GetProfitReportRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await profit.BuildAsync(ReportRange.Resolve(request.From, request.To, clock), request.GroupBy ?? ReportGroupBy.Day);
    }
}
