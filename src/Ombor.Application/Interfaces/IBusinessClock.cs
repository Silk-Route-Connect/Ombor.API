namespace Ombor.Application.Interfaces;

/// <summary>
/// The one source of "now" and of the business calendar. Ombor's businesses work on Tashkent time (Asia/Tashkent,
/// UTC+5, no daylight saving), so "today", due-date overdue checks, debt ages and the dashboard's day and hour buckets
/// follow the local day, not the UTC one — a sale rung up at 03:00 belongs to that local day. Timestamps are still
/// stored in UTC; only calendar questions go through here.
/// </summary>
public interface IBusinessClock
{
    /// <summary>The current instant.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>The business time zone.</summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>Today's local calendar date.</summary>
    DateOnly Today { get; }

    /// <summary>The local calendar date an instant falls on.</summary>
    DateOnly DateOf(DateTimeOffset instant);

    /// <summary>The instant a local calendar day begins (its local midnight), as a UTC-comparable timestamp.</summary>
    DateTimeOffset StartOfDay(DateOnly date);

    /// <summary>The instant expressed in local time (same moment, local offset) — for local hour-of-day.</summary>
    DateTimeOffset ToLocal(DateTimeOffset instant);
}
