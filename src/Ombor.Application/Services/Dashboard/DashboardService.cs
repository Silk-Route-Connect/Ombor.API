using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Services.DebtPositions;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Domain.Enums;
using DashboardPeriod = Ombor.Contracts.Enums.DashboardPeriod;

namespace Ombor.Application.Services.Dashboard;

internal sealed class DashboardService(
    IApplicationDbContext context,
    IOrganizationAccessor organization,
    IBusinessClock clock,
    DebtPositionCalculator debtPositions,
    DashboardSeriesBuilder seriesBuilder,
    DashboardMoney money) : IDashboardService
{
    private const int TopDebtorsCount = 5;
    private const int RecentTransactionsLimit = 10;

    public async Task<DashboardDto> GetAsync(DashboardPeriod period)
    {
        var window = DashboardWindow.Build(period, clock);

        var businessName = await BusinessNameAsync();
        var wallets = await context.Wallets
            .AsNoTracking()
            .OrderBy(w => w.Id)
            .Select(w => new DashboardWalletDto(w.Id, w.Name, w.Type.ToString()))
            .ToArrayAsync();

        var (series, revenue, saleRefunds) = await seriesBuilder.BuildAsync(window, [.. wallets.Select(w => w.Id)]);
        var debts = await debtPositions.ComputeAsync(window.TrendCutoffs);
        var cash = await money.CashAsync(window);
        var stockValue = await money.StockValueAsync();
        var recent = await RecentTransactionsAsync();

        return new DashboardDto(
            businessName,
            period.ToString().ToLowerInvariant(),
            revenue,
            DebtKpi(debts, t => t.Receivable, t => t.ReceivablePartners),
            DebtKpi(debts, t => t.Payable, t => t.PayablePartners),
            OverdueKpi(debts),
            series,
            wallets,
            [.. DebtAging.Buckets.Select((bucket, i) => new DashboardAgingBucketDto(bucket, debts.Totals.Aging[i]))],
            TopDebtors(debts),
            recent,
            saleRefunds,
            stockValue,
            cash);
    }

    /// <summary>
    /// A debt KPI from the net partner positions (the same figures as the debts summary). The trend is the position
    /// at the end of each bucket, ending on today's headline; deltaPct compares it with the start of the period.
    /// </summary>
    private static DashboardKpiDto DebtKpi(
        DebtPositionSnapshot debts, Func<DebtTotals, decimal> value, Func<DebtTotals, int> partners)
    {
        var current = value(debts.Totals);

        return new DashboardKpiDto(
            current,
            DashboardSeriesBuilder.DeltaPct(current, value(debts.AtCutoffs[0])),
            partners(debts.Totals),
            Trend(debts, value));
    }

    // «Долги старше 30 дней»: the part of the net receivable aged 31+ days (the aging definition, not due dates).
    private static DashboardOverdueKpiDto OverdueKpi(DebtPositionSnapshot debts) =>
        new(
            debts.Totals.OlderThan30Days,
            DashboardSeriesBuilder.DeltaPct(debts.Totals.OlderThan30Days, debts.AtCutoffs[0].OlderThan30Days),
            debts.Totals.OlderThan30DaysItems,
            Trend(debts, t => t.OlderThan30Days),
            debts.Totals.OlderThan30DaysPartners);

    private static decimal[] Trend(DebtPositionSnapshot debts, Func<DebtTotals, decimal> value) =>
        [.. debts.AtCutoffs.Skip(1).Select(value), value(debts.Totals)];

    private static DashboardDebtorDto[] TopDebtors(DebtPositionSnapshot debts) =>
        [.. debts.Partners
            .Where(p => p.Balance > 0m)
            .OrderByDescending(p => p.Balance)
            .ThenBy(p => p.Name)
            .Take(TopDebtorsCount)
            .Select(p => new DashboardDebtorDto(p.PartnerId, p.Name, p.Company, p.Balance))];

    private async Task<string> BusinessNameAsync()
    {
        if (organization.OrganizationId is not int organizationId)
        {
            return string.Empty;
        }

        return await context.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync() ?? string.Empty;
    }

    private async Task<DashboardRecentTransactionDto[]> RecentTransactionsAsync()
    {
        var recent = await context.Transactions
            .AsNoTracking()
            .Where(t => t.Type == TransactionType.Sale || t.Type == TransactionType.Supply)
            .OrderByDescending(t => t.DateUtc)
            .ThenByDescending(t => t.Id)
            .Take(RecentTransactionsLimit)
            .Select(t => new { t.Id, t.Number, t.DateUtc, t.PartnerId, PartnerName = t.Partner.Name, t.Type, t.TotalDue, t.TotalPaid })
            .ToArrayAsync();

        return [.. recent.Select(t => new DashboardRecentTransactionDto(
            t.Id, t.DateUtc, t.PartnerName, t.Type.ToString(), t.TotalDue, t.TotalPaid, PaymentStatusOf(t.TotalDue, t.TotalPaid),
            t.Number?.ToString(), t.PartnerId))];
    }

    private static string PaymentStatusOf(decimal totalDue, decimal totalPaid) =>
        totalPaid <= 0m ? "unpaid" : totalPaid < totalDue ? "partial" : "paid";
}
