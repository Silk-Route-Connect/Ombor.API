using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Report;
using PaymentType = Ombor.Domain.Enums.PaymentType;

namespace Ombor.Application.Services.Reports;

/// <summary>
/// The profit report — a simple P&amp;L: net revenue − cost of goods sold = gross profit; − losses − payroll − other
/// (General) expenses = profit. Each part is read by the same loader as its own report, so the P&amp;L for a period
/// agrees with the sales, losses and expenses reports for those days. Payments to partners settle debts rather than
/// cost anything (the goods' cost is already in COGS), so they are not expenses here.
/// </summary>
internal sealed class ProfitReportBuilder(ReportLines lines, LossesReportBuilder losses, ReportExpenses expenses)
{
    public async Task<ProfitReportDto> BuildAsync(ReportRange range, ReportGroupBy groupBy)
    {
        var sales = await lines.SalesAsync(range.StartUtc, range.EndUtc);
        var written = await losses.LoadAsync(range);
        var paid = (await expenses.LoadAsync(range.StartUtc, range.EndUtc))
            .Where(p => p.Type is PaymentType.Payroll or PaymentType.General)
            .ToArray();

        var salesByKey = sales.ToLookup(l => ReportBuckets.KeyOf(groupBy, l.Date));
        var lossesByKey = written.ToLookup(l => ReportBuckets.KeyOf(groupBy, l.Date));
        var paidByKey = paid.ToLookup(p => ReportBuckets.KeyOf(groupBy, p.Date));

        var rows = ReportBuckets.KeysOf(groupBy, range)
            .Select(key => Row(key, LineFigures.Of([.. salesByKey[key]]), [.. lossesByKey[key]], [.. paidByKey[key]]))
            .ToArray();
        var whole = Row(string.Empty, LineFigures.Of(sales), written, paid);

        return new ProfitReportDto(
            range.From,
            range.To,
            groupBy,
            rows,
            new ProfitReportTotalsDto(
                whole.Revenue,
                whole.Refunds,
                whole.NetRevenue,
                whole.Cost,
                whole.GrossProfit,
                whole.Losses,
                whole.Payroll,
                whole.OtherExpenses,
                whole.Profit),
            whole.CostIsEstimated);
    }

    private static ProfitReportRowDto Row(string key, LineFigures sales, ReportLoss[] losses, ReportExpense[] paid)
    {
        var lost = losses.Sum(l => l.Value);
        var payroll = paid.Where(p => p.Type == PaymentType.Payroll).Sum(p => p.Amount);
        var other = paid.Where(p => p.Type == PaymentType.General).Sum(p => p.Amount);

        return new ProfitReportRowDto(
            key,
            key,
            sales.Amount,
            sales.RefundAmount,
            sales.NetAmount,
            sales.Cost,
            sales.GrossProfit,
            lost,
            payroll,
            other,
            sales.GrossProfit - lost - payroll - other,
            sales.CostIsEstimated);
    }
}
