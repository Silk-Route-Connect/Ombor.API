using Ombor.Application.Services.Reports;
using Ombor.Contracts.Responses.Dashboard;

namespace Ombor.Application.Services.Dashboard;

/// <summary>
/// The dashboard's gross profit for the period: net revenue − cost of goods sold, from the snapshotted line costs —
/// the same lines and rule as the sales report, so the card and «Отчёты» agree for the same days.
/// </summary>
internal sealed class DashboardProfit(ReportLines lines)
{
    public async Task<DashboardGrossProfitDto> GrossProfitAsync(DashboardWindow window)
    {
        var current = await lines.SalesAsync(window.Start, window.End);
        var previous = await lines.SalesAsync(window.PrevStart, window.PrevEnd);

        var trend = new decimal[window.Buckets.Count];
        foreach (var bucket in current.GroupBy(l => window.IndexOf(l.DateUtc)).Where(g => g.Key >= 0))
        {
            trend[bucket.Key] = LineFigures.Of([.. bucket]).GrossProfit;
        }

        var figures = LineFigures.Of(current);

        return new DashboardGrossProfitDto(
            figures.GrossProfit,
            DashboardSeriesBuilder.DeltaPct(figures.GrossProfit, LineFigures.Of(previous).GrossProfit),
            trend,
            figures.MarginPercent,
            figures.CostIsEstimated || previous.Any(l => l.CostIsEstimated));
    }
}
