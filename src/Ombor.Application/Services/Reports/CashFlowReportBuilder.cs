using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Report;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.Reports;

/// <summary>
/// The cash-flow report: each wallet's balance at the start and end of the period and the money that moved in between —
/// payments by type, transfers, and the opening balance of a wallet created in the period (dated at its creation, as
/// the dashboard's cash trend dates it). Balances follow the wallet-balance rule (rule 15): only wallet-sourced payment
/// components and transfers move money, so a period ending today closes on the wallet's served balance.
/// </summary>
internal sealed class CashFlowReportBuilder(IApplicationDbContext context, IBusinessClock clock)
{
    private sealed record Flow(int WalletId, DateOnly Date, decimal Income, decimal Expense, decimal TransfersIn, decimal TransfersOut);

    public async Task<CashFlowReportDto> BuildAsync(ReportRange range, int? walletId)
    {
        var wallets = await context.Wallets
            .AsNoTracking()
            .Where(w => walletId == null || w.Id == walletId)
            .Select(w => new { w.Id, w.Name, w.Type, w.IsArchived, w.OpeningBalance, w.CreatedAt })
            .ToArrayAsync();
        var ids = wallets.Select(w => w.Id).ToArray();
        var before = await BalancesBeforeAsync(ids, range.StartUtc);

        var components = await context.PaymentComponents
            .AsNoTracking()
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.WalletId != null && ids.Contains(c.WalletId.Value)
                && c.Payment.DateUtc >= range.StartUtc && c.Payment.DateUtc < range.EndUtc)
            .Select(c => new { WalletId = c.WalletId!.Value, c.PaymentId, c.Amount, c.Payment.Type, c.Payment.Direction, c.Payment.DateUtc })
            .ToArrayAsync();
        var transfers = await context.WalletTransfers
            .AsNoTracking()
            .Where(t => (ids.Contains(t.FromWalletId) || ids.Contains(t.ToWalletId))
                && t.DateUtc >= range.StartUtc && t.DateUtc < range.EndUtc)
            .Select(t => new { t.FromWalletId, t.ToWalletId, t.Amount, t.DateUtc })
            .ToArrayAsync();

        var flows = components
            .Select(c => new Flow(
                c.WalletId,
                clock.DateOf(c.DateUtc),
                c.Direction == PaymentDirection.Income ? c.Amount : 0m,
                c.Direction == PaymentDirection.Expense ? c.Amount : 0m,
                0m,
                0m))
            .Concat(transfers.Where(t => ids.Contains(t.ToWalletId)).Select(t => new Flow(t.ToWalletId, clock.DateOf(t.DateUtc), 0m, 0m, t.Amount, 0m)))
            .Concat(transfers.Where(t => ids.Contains(t.FromWalletId)).Select(t => new Flow(t.FromWalletId, clock.DateOf(t.DateUtc), 0m, 0m, 0m, t.Amount)))
            .ToArray();

        bool CreatedBefore(DateTimeOffset createdAt) => createdAt < range.StartUtc;
        bool CreatedInside(DateTimeOffset createdAt) => createdAt >= range.StartUtc && createdAt < range.EndUtc;

        var walletRows = wallets
            .OrderBy(w => w.Name, StringComparer.Ordinal)
            .Select(w =>
            {
                var own = flows.Where(f => f.WalletId == w.Id).ToArray();
                var opening = (CreatedBefore(w.CreatedAt) ? w.OpeningBalance : 0m) + before.GetValueOrDefault(w.Id);
                var initial = CreatedInside(w.CreatedAt) ? w.OpeningBalance : 0m;
                var (income, expense, transfersIn, transfersOut) = Sum(own);

                return new CashFlowWalletDto(
                    w.Id, w.Name, w.Type.ToString(), w.IsArchived, opening, initial, income, expense, transfersIn, transfersOut,
                    opening + initial + income - expense + transfersIn - transfersOut);
            })
            .ToArray();

        var flowsByDay = flows.ToLookup(f => f.Date);
        var initialByDay = wallets
            .Where(w => CreatedInside(w.CreatedAt))
            .ToLookup(w => clock.DateOf(w.CreatedAt), w => w.OpeningBalance);
        var running = walletRows.Sum(w => w.Opening);
        var series = range.Days
            .Select(day =>
            {
                var (income, expense, transfersIn, transfersOut) = Sum(flowsByDay[day]);
                var initial = initialByDay[day].Sum();
                running += initial + income - expense + transfersIn - transfersOut;

                return new CashFlowDayDto(day, income, expense, transfersIn, transfersOut, initial, running);
            })
            .ToArray();

        var byType = Enum.GetValues<PaymentType>()
            .Select(type =>
            {
                var ofType = components.Where(c => c.Type == type).ToArray();

                return new CashFlowTypeDto(
                    type.ToContractType(),
                    ofType.Where(c => c.Direction == PaymentDirection.Income).Sum(c => c.Amount),
                    ofType.Where(c => c.Direction == PaymentDirection.Expense).Sum(c => c.Amount),
                    ofType.Select(c => c.PaymentId).Distinct().Count());
            })
            .ToArray();

        var totals = new CashFlowTotalsDto(
            walletRows.Sum(w => w.Opening),
            walletRows.Sum(w => w.InitialBalance),
            walletRows.Sum(w => w.Income),
            walletRows.Sum(w => w.Expense),
            walletRows.Sum(w => w.TransfersIn),
            walletRows.Sum(w => w.TransfersOut),
            walletRows.Sum(w => w.Closing));

        return new CashFlowReportDto(range.From, range.To, walletRows, byType, series, totals);
    }

    private static (decimal Income, decimal Expense, decimal TransfersIn, decimal TransfersOut) Sum(IEnumerable<Flow> flows)
    {
        decimal income = 0m, expense = 0m, transfersIn = 0m, transfersOut = 0m;

        foreach (var flow in flows)
        {
            income += flow.Income;
            expense += flow.Expense;
            transfersIn += flow.TransfersIn;
            transfersOut += flow.TransfersOut;
        }

        return (income, expense, transfersIn, transfersOut);
    }

    /// <summary>Per wallet, the net of every payment and transfer dated before <paramref name="start"/>, summed in SQL.</summary>
    private async Task<Dictionary<int, decimal>> BalancesBeforeAsync(int[] ids, DateTimeOffset start)
    {
        var payments = await context.PaymentComponents
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.WalletId != null && ids.Contains(c.WalletId.Value)
                && c.Payment.DateUtc < start)
            .GroupBy(c => new { WalletId = c.WalletId!.Value, c.Payment.Direction })
            .Select(g => new { g.Key.WalletId, g.Key.Direction, Amount = g.Sum(c => c.Amount) })
            .ToArrayAsync();
        var received = await context.WalletTransfers
            .Where(t => ids.Contains(t.ToWalletId) && t.DateUtc < start)
            .GroupBy(t => t.ToWalletId)
            .Select(g => new { WalletId = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToArrayAsync();
        var sent = await context.WalletTransfers
            .Where(t => ids.Contains(t.FromWalletId) && t.DateUtc < start)
            .GroupBy(t => t.FromWalletId)
            .Select(g => new { WalletId = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToArrayAsync();

        return payments.Select(p => (p.WalletId, Amount: p.Direction == PaymentDirection.Income ? p.Amount : -p.Amount))
            .Concat(received.Select(r => (r.WalletId, r.Amount)))
            .Concat(sent.Select(s => (s.WalletId, Amount: -s.Amount)))
            .GroupBy(x => x.WalletId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
    }
}
