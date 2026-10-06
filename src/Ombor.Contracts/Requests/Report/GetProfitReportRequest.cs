using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Report;

/// <summary>The profit report (a simple P&amp;L) for a period.</summary>
/// <param name="From">First local (Tashkent) calendar day, inclusive; default the first day of <paramref name="To"/>'s month.</param>
/// <param name="To">Last local calendar day, inclusive; default today.</param>
/// <param name="GroupBy"><see cref="ReportGroupBy.Day"/>, <see cref="ReportGroupBy.Week"/> or <see cref="ReportGroupBy.Month"/>; default Day.</param>
public sealed record GetProfitReportRequest(
    DateOnly? From = null,
    DateOnly? To = null,
    ReportGroupBy? GroupBy = null);
