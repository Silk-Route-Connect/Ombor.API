using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;

namespace Ombor.Application.Services;

internal sealed class BusinessClock(TimeProvider time) : IBusinessClock
{
    public DateTimeOffset UtcNow => time.GetUtcNow();

    public TimeZoneInfo TimeZone => BusinessTimeZone.Tashkent;

    public DateOnly Today => DateOf(UtcNow);

    public DateOnly DateOf(DateTimeOffset instant) => DateOnly.FromDateTime(ToLocal(instant).DateTime);

    public DateTimeOffset StartOfDay(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var offset = TimeZone.GetUtcOffset(localMidnight);

        // An unset date (DateOnly.MinValue, e.g. an opening date from before it was recorded) has no UTC instant
        // five hours earlier; it simply means "before everything".
        return localMidnight - DateTime.MinValue < offset
            ? DateTimeOffset.MinValue
            : new DateTimeOffset(localMidnight, offset);
    }

    public DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, TimeZone);
}
