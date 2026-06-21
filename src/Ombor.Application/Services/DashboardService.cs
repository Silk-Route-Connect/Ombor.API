using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Domain.Enums;
using DashboardPeriod = Ombor.Contracts.Enums.DashboardPeriod;

namespace Ombor.Application.Services;

internal sealed class DashboardService(
    IApplicationDbContext context,
    IOrganizationAccessor organization,
    IDebtService debtService) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(DashboardPeriod period)
    {
        var now = DateTimeOffset.UtcNow;
        var window = BuildWindow(period, now);

        var businessName = await BusinessNameAsync();
        var wallets = await context.Wallets
            .OrderBy(w => w.Id)
            .Select(w => new { w.Id, w.Name, w.Type })
            .ToArrayAsync();

        var (series, revenue) = await BuildSeriesAndRevenueAsync(window, wallets.Select(w => w.Id).ToArray());
        var (receivable, payable, overdue, aging, topDebtors) = await BuildDebtFiguresAsync();
        var recent = await RecentTransactionsAsync();

        return new DashboardDto(
            businessName,
            period.ToString().ToLowerInvariant(),
            revenue,
            receivable,
            payable,
            overdue,
            series,
            [.. wallets.Select(w => new DashboardWalletDto(w.Id, w.Name, w.Type.ToString()))],
            aging,
            topDebtors,
            recent);
    }

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

    private async Task<(DashboardSeriesPointDto[] Series, DashboardKpiDto Revenue)> BuildSeriesAndRevenueAsync(
        Window window, int[] walletIds)
    {
        var buckets = window.Buckets;
        var walletIndex = new Dictionary<int, int>(walletIds.Length);
        for (var i = 0; i < walletIds.Length; i++)
        {
            walletIndex[walletIds[i]] = i;
        }

        var sales = new decimal[buckets.Count];
        var supplies = new decimal[buckets.Count];
        var payin = new decimal[buckets.Count];
        var payout = new decimal[buckets.Count];
        var walletPayin = NewMatrix(buckets.Count, walletIds.Length);
        var walletPayout = NewMatrix(buckets.Count, walletIds.Length);

        var transactions = await context.Transactions
            .Where(t => (t.Type == TransactionType.Sale || t.Type == TransactionType.Supply)
                && t.DateUtc >= window.Start && t.DateUtc < window.End)
            .Select(t => new { t.Type, t.DateUtc, t.TotalDue })
            .ToArrayAsync();

        var salesCount = 0;
        foreach (var t in transactions)
        {
            var i = window.IndexOf(t.DateUtc);
            if (i < 0)
            {
                continue;
            }

            if (t.Type == TransactionType.Sale)
            {
                sales[i] += t.TotalDue;
                salesCount++;
            }
            else
            {
                supplies[i] += t.TotalDue;
            }
        }

        var components = await context.PaymentComponents
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.WalletId != null
                && c.Payment.DateUtc >= window.Start && c.Payment.DateUtc < window.End)
            .Select(c => new { c.Amount, WalletId = c.WalletId!.Value, c.Payment.Direction, c.Payment.DateUtc })
            .ToArrayAsync();

        foreach (var c in components)
        {
            var i = window.IndexOf(c.DateUtc);
            if (i < 0 || !walletIndex.TryGetValue(c.WalletId, out var w))
            {
                continue;
            }

            if (c.Direction == PaymentDirection.Income)
            {
                payin[i] += c.Amount;
                walletPayin[i][w] += c.Amount;
            }
            else
            {
                payout[i] += c.Amount;
                walletPayout[i][w] += c.Amount;
            }
        }

        var series = new DashboardSeriesPointDto[buckets.Count];
        for (var i = 0; i < buckets.Count; i++)
        {
            series[i] = new DashboardSeriesPointDto(
                buckets[i].Label, sales[i], supplies[i], payin[i], payout[i], walletPayin[i], walletPayout[i]);
        }

        var revenueValue = sales.Sum();
        var prevRevenue = await context.Transactions
            .Where(t => t.Type == TransactionType.Sale && t.DateUtc >= window.PrevStart && t.DateUtc < window.PrevEnd)
            .SumAsync(t => (decimal?)t.TotalDue) ?? 0m;
        var deltaPct = prevRevenue == 0m ? (decimal?)null : Math.Round((revenueValue - prevRevenue) / prevRevenue * 100m, 2);

        var revenue = new DashboardKpiDto(revenueValue, deltaPct, salesCount, sales);

        return (series, revenue);
    }

    private async Task<(DashboardKpiDto Receivable, DashboardKpiDto Payable, DashboardOverdueKpiDto Overdue,
        DashboardAgingBucketDto[] Aging, DashboardDebtorDto[] TopDebtors)> BuildDebtFiguresAsync()
    {
        // Derived from the same source as GET /api/debts, so the two screens reconcile by construction (§K).
        var debts = await debtService.GetDebtsAsync();

        var receivables = debts.Where(d => d.Direction == DebtDirections.Receivable).ToArray();
        var payables = debts.Where(d => d.Direction == DebtDirections.Payable).ToArray();

        var receivable = new DashboardKpiDto(receivables.Sum(d => d.Remaining), null, receivables.Length, []);
        var payable = new DashboardKpiDto(payables.Sum(d => d.Remaining), null, payables.Length, []);

        // «Просрочено» = receivables aged 31+ days (the aging definition, not due-date overdue — §K).
        var overdueRows = receivables.Where(d => d.AgeDays >= 31).ToArray();
        var overdue = new DashboardOverdueKpiDto(
            overdueRows.Sum(d => d.Remaining),
            null,
            overdueRows.Length,
            [],
            overdueRows.Select(d => d.PartnerId).Distinct().Count());

        DashboardAgingBucketDto Bucket(string label, Func<int, bool> inRange) =>
            new(label, receivables.Where(d => inRange(d.AgeDays)).Sum(d => d.Remaining));

        DashboardAgingBucketDto[] aging =
        [
            Bucket("0-7", age => age <= 7),
            Bucket("8-30", age => age is >= 8 and <= 30),
            Bucket("31-60", age => age is >= 31 and <= 60),
            Bucket("60+", age => age > 60),
        ];

        var topDebtors = receivables
            .GroupBy(d => new { d.PartnerId, d.PartnerName, d.PartnerCompany })
            .Select(g => new DashboardDebtorDto(g.Key.PartnerId, g.Key.PartnerName, g.Key.PartnerCompany, g.Sum(d => d.Remaining)))
            .OrderByDescending(x => x.Amount)
            .Take(TopDebtorsCount)
            .ToArray();

        return (receivable, payable, overdue, aging, topDebtors);
    }

    private async Task<DashboardRecentTransactionDto[]> RecentTransactionsAsync()
    {
        var recent = await context.Transactions
            .Where(t => t.Type == TransactionType.Sale || t.Type == TransactionType.Supply)
            .OrderByDescending(t => t.DateUtc)
            .ThenByDescending(t => t.Id)
            .Take(RecentTransactionsLimit)
            .Select(t => new { t.Id, t.DateUtc, PartnerName = t.Partner.Name, t.Type, t.TotalDue, t.TotalPaid })
            .ToArrayAsync();

        return [.. recent.Select(t => new DashboardRecentTransactionDto(
            t.Id, t.DateUtc, t.PartnerName, t.Type.ToString(), t.TotalDue, t.TotalPaid, PaymentStatusOf(t.TotalDue, t.TotalPaid)))];
    }

    private static string PaymentStatusOf(decimal totalDue, decimal totalPaid) =>
        totalPaid <= 0m ? "unpaid" : totalPaid < totalDue ? "partial" : "paid";

    private static decimal[][] NewMatrix(int rows, int cols)
    {
        var matrix = new decimal[rows][];
        for (var i = 0; i < rows; i++)
        {
            matrix[i] = new decimal[cols];
        }

        return matrix;
    }

    // Rolling windows anchored at "now" (UTC), so deltaPct compares like-for-like with the preceding equal span.
    private static Window BuildWindow(DashboardPeriod period, DateTimeOffset now)
    {
        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

        if (period == DashboardPeriod.Today)
        {
            var end = dayStart.AddDays(1);
            var buckets = new List<Bucket>(24);
            for (var h = 0; h < 24; h++)
            {
                buckets.Add(new Bucket(h.ToString("00")));
            }

            return new Window(buckets, dayStart, end, dayStart.AddDays(-1), dayStart, Hourly: true);
        }

        var days = period == DashboardPeriod.Week ? 7 : 30;
        var start = dayStart.AddDays(-(days - 1));
        var dailyBuckets = new List<Bucket>(days);
        for (var d = 0; d < days; d++)
        {
            dailyBuckets.Add(new Bucket(start.AddDays(d).ToString("yyyy-MM-dd")));
        }

        return new Window(dailyBuckets, start, dayStart.AddDays(1), start.AddDays(-days), start, Hourly: false);
    }

    private const int TopDebtorsCount = 5;
    private const int RecentTransactionsLimit = 10;

    private sealed record Bucket(string Label);

    private sealed record Window(
        IReadOnlyList<Bucket> Buckets,
        DateTimeOffset Start,
        DateTimeOffset End,
        DateTimeOffset PrevStart,
        DateTimeOffset PrevEnd,
        bool Hourly)
    {
        /// <summary>The bucket index for a timestamp in [Start, End); -1 if out of range.</summary>
        public int IndexOf(DateTimeOffset at)
        {
            var index = Hourly
                ? (int)Math.Floor((at - Start).TotalHours)
                : (at.UtcDateTime.Date - Start.UtcDateTime.Date).Days;

            return index >= 0 && index < Buckets.Count ? index : -1;
        }
    }
}
