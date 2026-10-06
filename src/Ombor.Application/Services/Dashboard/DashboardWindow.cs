using System.Globalization;
using Ombor.Application.Interfaces;
using Ombor.Application.Services.DebtPositions;
using DashboardPeriod = Ombor.Contracts.Enums.DashboardPeriod;

namespace Ombor.Application.Services.Dashboard;

/// <summary>One bucket of the dashboard series: [<see cref="Start"/>, <see cref="End"/>) on the local calendar.</summary>
internal sealed record DashboardBucket(string Label, DateTimeOffset Start, DateTimeOffset End, DateOnly Date);

/// <summary>
/// The dashboard period laid out on the business (Tashkent) calendar: «today» is 24 local hours from local midnight,
/// «week» / «month» the last 7 / 30 local days ending today. The preceding equal span is the basis for deltaPct.
/// </summary>
internal sealed record DashboardWindow(
    IReadOnlyList<DashboardBucket> Buckets,
    DateTimeOffset PrevStart,
    DateOnly PrevLastDate)
{
    public DateTimeOffset Start => Buckets[0].Start;

    public DateTimeOffset End => Buckets[^1].End;

    public DateTimeOffset PrevEnd => Start;

    /// <summary>
    /// The points the debt and cash trends are measured at: first the start of the period (the delta basis), then the
    /// end of every bucket but the last — the last point is today's headline itself.
    /// </summary>
    public IReadOnlyList<DebtCutoff> TrendCutoffs =>
        [new DebtCutoff(Start, PrevLastDate), .. Buckets.Take(Buckets.Count - 1).Select(b => new DebtCutoff(b.End, b.Date))];

    /// <summary>The bucket index for a timestamp in [Start, End); -1 if out of range.</summary>
    public int IndexOf(DateTimeOffset at)
    {
        for (var i = 0; i < Buckets.Count; i++)
        {
            if (at >= Buckets[i].Start && at < Buckets[i].End)
            {
                return i;
            }
        }

        return -1;
    }

    public static DashboardWindow Build(DashboardPeriod period, IBusinessClock clock)
    {
        var today = clock.Today;

        if (period == DashboardPeriod.Today)
        {
            var dayStart = clock.StartOfDay(today);
            var hours = Enumerable.Range(0, 24)
                .Select(h => new DashboardBucket(h.ToString("00", CultureInfo.InvariantCulture), dayStart.AddHours(h), dayStart.AddHours(h + 1), today))
                .ToArray();

            return new DashboardWindow(hours, clock.StartOfDay(today.AddDays(-1)), today.AddDays(-1));
        }

        var days = period == DashboardPeriod.Week ? 7 : 30;
        var first = today.AddDays(-(days - 1));
        var buckets = Enumerable.Range(0, days)
            .Select(d => first.AddDays(d))
            .Select(date => new DashboardBucket(
                date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), clock.StartOfDay(date), clock.StartOfDay(date.AddDays(1)), date))
            .ToArray();

        return new DashboardWindow(buckets, clock.StartOfDay(first.AddDays(-days)), first.AddDays(-1));
    }
}
