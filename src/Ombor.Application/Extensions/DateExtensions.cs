namespace Ombor.Application.Extensions;

internal static class DateExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime, TimeSpan offset)
        => new(dateTime, offset);

    public static DateTimeOffset ToUtcDateTimeOffset(this DateOnly date)
        => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public static DateTimeOffset ToUtcDateTimeOffset(this DateTime dateTime)
        => new(dateTime, TimeSpan.Zero);

    public static DateTime ToLocalDateTime(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.LocalDateTime;
}
