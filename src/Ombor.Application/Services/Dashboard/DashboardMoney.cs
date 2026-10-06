using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.Dashboard;

/// <summary>
/// The dashboard's money-on-hand figures: cash across wallets and the value of stock. Archived wallets and warehouses
/// still holding money or goods count (rule 31).
/// </summary>
internal sealed class DashboardMoney(IApplicationDbContext context)
{
    public async Task<DashboardCashDto> CashAsync(DashboardWindow window)
    {
        var wallets = await context.Wallets
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new { w.Id, w.Name, w.Type, w.IsArchived, w.OpeningBalance, w.CreatedAt })
            .ToArrayAsync();
        var balances = await context.Wallets.ComputeWalletBalancesAsync();
        var total = wallets.Sum(w => balances.GetValueOrDefault(w.Id));

        // Cash at a past moment = today's cash with everything dated since undone. Transfers between wallets move
        // nothing in total, so only payments and newly opened wallets change it.
        var since = window.Start;
        var flows = await context.PaymentComponents
            .AsNoTracking()
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.WalletId != null && c.Payment.DateUtc >= since)
            .Select(c => new { c.Payment.DateUtc, Amount = c.Payment.Direction == PaymentDirection.Income ? c.Amount : -c.Amount })
            .ToArrayAsync();

        decimal CashBefore(DateTimeOffset cutoff) =>
            total
            - flows.Where(f => f.DateUtc >= cutoff).Sum(f => f.Amount)
            - wallets.Where(w => w.CreatedAt >= cutoff).Sum(w => w.OpeningBalance);

        var cutoffs = window.TrendCutoffs;
        var trend = cutoffs.Skip(1).Select(c => CashBefore(c.Before)).Append(total).ToArray();

        return new DashboardCashDto(
            total,
            DashboardSeriesBuilder.DeltaPct(total, CashBefore(cutoffs[0].Before)),
            trend,
            [.. wallets.Select(w => new DashboardWalletBalanceDto(w.Id, w.Name, w.Type.ToString(), balances.GetValueOrDefault(w.Id), w.IsArchived))]);
    }

    public async Task<DashboardStockValueDto> StockValueAsync()
    {
        var items = await context.WarehouseItems
            .AsNoTracking()
            .Where(i => i.Quantity > 0m)
            .Select(i => new { i.ProductId, i.WarehouseId, i.Quantity, i.AverageCost })
            .ToArrayAsync();

        // The same carrying value as WarehouseDto.StockValue (Σ quantity × WAC), summed over every warehouse.
        return new DashboardStockValueDto(
            Math.Round(items.Sum(i => i.Quantity * i.AverageCost), 2, MidpointRounding.AwayFromZero),
            items.Select(i => i.ProductId).Distinct().Count(),
            items.Select(i => i.WarehouseId).Distinct().Count());
    }
}
