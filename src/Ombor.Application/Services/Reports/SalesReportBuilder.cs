using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Report;

namespace Ombor.Application.Services.Reports;

/// <summary>The sales report: sales net of refunds, their snapshotted cost and the gross profit, per row and in total.</summary>
internal sealed class SalesReportBuilder(ReportLines lines, ReportGrouping grouping)
{
    public async Task<SalesReportDto> BuildAsync(ReportRange range, ReportGroupBy groupBy)
    {
        var sales = await lines.SalesAsync(range.StartUtc, range.EndUtc);
        var groups = await grouping.GroupAsync(sales, groupBy, range);
        var rows = groups.Select(g => Row(g.Key, g.Label, LineFigures.Of(g.Lines)));

        if (!ReportBuckets.IsTime(groupBy))
        {
            rows = rows.OrderByDescending(r => r.NetRevenue).ThenBy(r => r.Label, StringComparer.Ordinal);
        }

        var totals = LineFigures.Of(sales);

        return new SalesReportDto(
            range.From,
            range.To,
            groupBy,
            [.. rows],
            new SalesReportTotalsDto(
                totals.Documents,
                totals.RefundDocuments,
                totals.Quantity,
                totals.Amount,
                totals.RefundAmount,
                totals.NetAmount,
                totals.Cost,
                totals.GrossProfit,
                totals.MarginPercent),
            totals.CostIsEstimated);
    }

    private static SalesReportRowDto Row(string key, string label, LineFigures figures) =>
        new(
            key,
            label,
            figures.Documents,
            figures.RefundDocuments,
            figures.Quantity,
            figures.Amount,
            figures.RefundAmount,
            figures.NetAmount,
            figures.Cost,
            figures.GrossProfit,
            figures.MarginPercent,
            figures.CostIsEstimated);
}
