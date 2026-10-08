namespace Ombor.Tests.Common.Helpers;

/// <summary>
/// The business calendar as tests see it — Tashkent time (UTC+5), like the API's clock. Tests that pick due dates or
/// back-date documents use this, never the UTC date, or they flake for the five hours each day the two disagree.
/// </summary>
public static class BusinessDay
{
    public static TimeZoneInfo Zone { get; } =
        TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Tashkent", out var zone) ? zone
        : TimeZoneInfo.TryFindSystemTimeZoneById("West Asia Standard Time", out zone) ? zone
        : TimeZoneInfo.CreateCustomTimeZone("Asia/Tashkent", TimeSpan.FromHours(5), "Tashkent", "Tashkent");

    public static DateOnly Today => DateOf(DateTimeOffset.UtcNow);

    public static DateOnly DateOf(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    /// <summary>The instant the local day begins.</summary>
    public static DateTimeOffset StartOfDay(DateOnly date)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        return new DateTimeOffset(midnight, Zone.GetUtcOffset(midnight));
    }
}
