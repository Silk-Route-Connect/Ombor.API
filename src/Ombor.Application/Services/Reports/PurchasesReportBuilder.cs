using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Report;

namespace Ombor.Application.Services.Reports;

/// <summary>The purchases report: supplies net of goods returned to suppliers, per row and in total.</summary>
internal sealed class PurchasesReportBuilder(ReportLines lines, ReportGrouping grouping)
{
    public async Task<PurchasesReportDto> BuildAsync(ReportRange range, ReportGroupBy groupBy)
    {
        var purchases = await lines.PurchasesAsync(range.StartUtc, range.EndUtc);
        var groups = await grouping.GroupAsync(purchases, groupBy, range);
        var rows = groups.Select(g => Row(g.Key, g.Label, LineFigures.Of(g.Lines)));

        if (!ReportBuckets.IsTime(groupBy))
        {
            rows = rows.OrderByDescending(r => r.NetPurchases).ThenBy(r => r.Label, StringComparer.Ordinal);
        }

        var totals = LineFigures.Of(purchases);

        return new PurchasesReportDto(
            range.From,
            range.To,
            groupBy,
            [.. rows],
            new PurchasesReportTotalsDto(
                totals.Documents,
                totals.RefundDocuments,
                totals.Quantity,
                totals.Amount,
                totals.RefundAmount,
                totals.NetAmount));
    }

    private static PurchasesReportRowDto Row(string key, string label, LineFigures figures) =>
        new(
            key,
            label,
            figures.Documents,
            figures.RefundDocuments,
            figures.Quantity,
            figures.Amount,
            figures.RefundAmount,
            figures.NetAmount);
}
