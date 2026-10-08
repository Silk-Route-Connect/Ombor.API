namespace Ombor.Contracts.Requests.Report;

/// <summary>A report over a period with no other options (expenses, losses).</summary>
/// <param name="From">First local (Tashkent) calendar day, inclusive; default the first day of <paramref name="To"/>'s month.</param>
/// <param name="To">Last local calendar day, inclusive; default today.</param>
public sealed record GetReportPeriodRequest(
    DateOnly? From = null,
    DateOnly? To = null);
