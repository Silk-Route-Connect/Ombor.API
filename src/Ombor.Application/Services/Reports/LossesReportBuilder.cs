using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Report;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.Reports;

/// <summary>A decrease adjustment dated on the local calendar, valued like the adjustments list serves it.</summary>
internal sealed record ReportLoss(DateOnly Date, int ProductId, string Reason, decimal Quantity, decimal Value);

/// <summary>
/// The losses report: stock written off by decrease adjustments (rule 24), valued at the WAC snapshotted on each, by
/// product and by reason. An increase is not netted against it — it carries no link to the decrease it may correct.
/// </summary>
internal sealed class LossesReportBuilder(IApplicationDbContext context, IBusinessClock clock)
{
    public async Task<LossesReportDto> BuildAsync(ReportRange range)
    {
        var losses = await LoadAsync(range);
        var productIds = losses.Select(l => l.ProductId).Distinct().ToArray();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.SKU, p.Measurement })
            .ToDictionaryAsync(p => p.Id);

        var byProduct = losses
            .GroupBy(l => l.ProductId)
            .Select(g => new LossProductDto(
                g.Key,
                products[g.Key].Name,
                products[g.Key].SKU,
                products[g.Key].Measurement.ToString(),
                g.Sum(l => l.Quantity),
                g.Sum(l => l.Value),
                g.Count()))
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.ProductName, StringComparer.Ordinal)
            .ToArray();

        var byReason = losses
            .GroupBy(l => l.Reason, StringComparer.OrdinalIgnoreCase)
            .Select(g => new LossReasonDto(g.First().Reason, g.Sum(l => l.Value), g.Count()))
            .OrderByDescending(r => r.Value)
            .ThenBy(r => r.Reason, StringComparer.Ordinal)
            .ToArray();

        return new LossesReportDto(range.From, range.To, losses.Sum(l => l.Value), losses.Length, byProduct, byReason);
    }

    /// <summary>Decrease adjustments dated in the period.</summary>
    public async Task<ReportLoss[]> LoadAsync(ReportRange range)
    {
        var rows = await context.StockAdjustments
            .AsNoTracking()
            .Where(a => a.Direction == StockAdjustmentDirection.Decrease
                && a.DateUtc >= range.StartUtc && a.DateUtc < range.EndUtc)
            .Select(a => new { a.DateUtc, a.ProductId, a.Reason, a.Quantity, a.UnitCost })
            .ToArrayAsync();

        return [.. rows.Select(a => new ReportLoss(
            clock.DateOf(a.DateUtc),
            a.ProductId,
            a.Reason,
            a.Quantity,
            Math.Round(a.Quantity * a.UnitCost, 2, MidpointRounding.AwayFromZero)))];
    }
}
