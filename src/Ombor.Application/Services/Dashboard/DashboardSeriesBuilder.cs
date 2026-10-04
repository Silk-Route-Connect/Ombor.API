using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.Dashboard;

/// <summary>The period-driven part of the dashboard: the per-bucket series and the revenue and refund KPIs.</summary>
internal sealed class DashboardSeriesBuilder(IApplicationDbContext context)
{
    public async Task<(DashboardSeriesPointDto[] Series, DashboardKpiDto Revenue, DashboardKpiDto SaleRefunds)> BuildAsync(
        DashboardWindow window, int[] walletIds)
    {
        var count = window.Buckets.Count;
        var walletIndex = walletIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);

        var sales = new decimal[count];
        var supplies = new decimal[count];
        var refunds = new decimal[count];
        var payin = new decimal[count];
        var payout = new decimal[count];
        var walletPayin = NewMatrix(count, walletIds.Length);
        var walletPayout = NewMatrix(count, walletIds.Length);

        var transactions = await context.Transactions
            .AsNoTracking()
            .Where(t => (t.Type == TransactionType.Sale || t.Type == TransactionType.Supply || t.Type == TransactionType.SaleRefund)
                && t.DateUtc >= window.Start && t.DateUtc < window.End)
            .Select(t => new { t.Type, t.DateUtc, t.TotalDue })
            .ToArrayAsync();

        int salesCount = 0, refundCount = 0;
        foreach (var t in transactions)
        {
            var i = window.IndexOf(t.DateUtc);
            if (i < 0)
            {
                continue;
            }

            switch (t.Type)
            {
                case TransactionType.Sale:
                    sales[i] += t.TotalDue;
                    salesCount++;
                    break;
                case TransactionType.SaleRefund:
                    refunds[i] += t.TotalDue;
                    refundCount++;
                    break;
                default:
                    supplies[i] += t.TotalDue;
                    break;
            }
        }

        var components = await context.PaymentComponents
            .AsNoTracking()
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

        var series = window.Buckets
            .Select((bucket, i) => new DashboardSeriesPointDto(
                bucket.Label, sales[i], supplies[i], payin[i], payout[i], walletPayin[i], walletPayout[i], refunds[i]))
            .ToArray();

        var previous = await context.Transactions
            .AsNoTracking()
            .Where(t => (t.Type == TransactionType.Sale || t.Type == TransactionType.SaleRefund)
                && t.DateUtc >= window.PrevStart && t.DateUtc < window.PrevEnd)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.TotalDue) })
            .ToArrayAsync();
        var previousSales = previous.Where(p => p.Type == TransactionType.Sale).Sum(p => p.Total);
        var previousRefunds = previous.Where(p => p.Type == TransactionType.SaleRefund).Sum(p => p.Total);

        // Revenue is net of sale refunds, so it reconciles with the Sales list, where a refund is a negative row.
        var netTrend = sales.Zip(refunds, (sale, refund) => sale - refund).ToArray();
        var revenue = new DashboardKpiDto(netTrend.Sum(), DeltaPct(netTrend.Sum(), previousSales - previousRefunds), salesCount, netTrend);
        var saleRefunds = new DashboardKpiDto(refunds.Sum(), DeltaPct(refunds.Sum(), previousRefunds), refundCount, refunds);

        return (series, revenue, saleRefunds);
    }

    /// <summary>Percent change vs a basis; null when there is no basis to compare with.</summary>
    public static decimal? DeltaPct(decimal current, decimal basis) =>
        basis == 0m ? null : Math.Round((current - basis) / Math.Abs(basis) * 100m, 2);

    private static decimal[][] NewMatrix(int rows, int cols) =>
        [.. Enumerable.Range(0, rows).Select(_ => new decimal[cols])];
}
