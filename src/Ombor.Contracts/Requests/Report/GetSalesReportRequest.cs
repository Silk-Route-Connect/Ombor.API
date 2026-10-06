using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Report;

/// <summary>The sales report: sales and sale refunds in a period, with cost and gross profit.</summary>
/// <param name="From">First local (Tashkent) calendar day, inclusive; default the first day of <paramref name="To"/>'s month.</param>
/// <param name="To">Last local calendar day, inclusive; default today.</param>
/// <param name="GroupBy">How rows are split; default <see cref="ReportGroupBy.Day"/>.</param>
public sealed record GetSalesReportRequest(
    DateOnly? From = null,
    DateOnly? To = null,
    ReportGroupBy? GroupBy = null);
