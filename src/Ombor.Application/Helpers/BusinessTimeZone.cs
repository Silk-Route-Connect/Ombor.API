namespace Ombor.Application.Helpers;

/// <summary>
/// Resolves the business time zone once. The IANA id works on Linux and on Windows with ICU; the Windows id is the
/// fallback (note: Tashkent is «West Asia Standard Time» — «Central Asia Standard Time» is Astana). If neither is
/// installed, a fixed UTC+5 zone stands in: Uzbekistan has kept UTC+5 without daylight saving since 1992.
/// </summary>
internal static class BusinessTimeZone
{
    private static readonly string[] Ids = ["Asia/Tashkent", "West Asia Standard Time"];

    public static TimeZoneInfo Tashkent { get; } = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in Ids)
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Asia/Tashkent", TimeSpan.FromHours(5), "Tashkent", "Tashkent");
    }
}
