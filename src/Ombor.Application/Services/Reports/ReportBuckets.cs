using System.Globalization;
using Ombor.Contracts.Enums;

namespace Ombor.Application.Services.Reports;

/// <summary>
/// The time buckets of a report: a day is <c>yyyy-MM-dd</c>, a week is keyed by its Monday (<c>yyyy-MM-dd</c>), a month
/// is <c>yyyy-MM</c>. The keys sort chronologically as text, and the key doubles as the label (the client formats it).
/// </summary>
internal static class ReportBuckets
{
    public static bool IsTime(ReportGroupBy groupBy) =>
        groupBy is ReportGroupBy.Day or ReportGroupBy.Week or ReportGroupBy.Month;

    public static string KeyOf(ReportGroupBy groupBy, DateOnly date) => groupBy switch
    {
        ReportGroupBy.Day => Format(date),
        ReportGroupBy.Week => Format(MondayOf(date)),
        ReportGroupBy.Month => date.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        _ => throw new ArgumentOutOfRangeException(nameof(groupBy), groupBy, "Not a time grouping."),
    };

    /// <summary>Every bucket the period touches, oldest first — a bucket only partly inside the period included.</summary>
    public static string[] KeysOf(ReportGroupBy groupBy, ReportRange range) =>
        [.. range.Days.Select(day => KeyOf(groupBy, day)).Distinct()];

    private static DateOnly MondayOf(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static string Format(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
