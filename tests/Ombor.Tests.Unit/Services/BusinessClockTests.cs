using Ombor.Application.Services;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services;

/// <summary>backend-20: the business day is Tashkent's (UTC+5), not UTC's.</summary>
public sealed class BusinessClockTests
{
    [Fact]
    public void Today_ShouldBeTheLocalDate_BeforeUtcMidnightCatchesUp()
    {
        // 03.10 22:30 UTC is already 04.10 03:30 in Tashkent.
        var clock = new BusinessClock(new FixedTimeProvider(new DateTimeOffset(2026, 10, 3, 22, 30, 0, TimeSpan.Zero)));

        Assert.Equal(new DateOnly(2026, 10, 4), clock.Today);
    }

    [Fact]
    public void StartOfDay_ShouldBeLocalMidnight()
    {
        var clock = new BusinessClock(TimeProvider.System);

        var start = clock.StartOfDay(new DateOnly(2026, 10, 4));

        Assert.Equal(new DateTimeOffset(2026, 10, 3, 19, 0, 0, TimeSpan.Zero), start.ToUniversalTime());
        Assert.Equal(TimeSpan.FromHours(5), start.Offset);
    }

    [Fact]
    public void StartOfDay_ShouldNotThrow_ForAnUnsetDate()
    {
        var clock = new BusinessClock(TimeProvider.System);

        Assert.Equal(DateTimeOffset.MinValue, clock.StartOfDay(DateOnly.MinValue));
    }

    [Theory]
    [InlineData(18, 59, 3)] // 23:59 local on 03.10
    [InlineData(19, 0, 4)]  // 00:00 local on 04.10
    public void DateOf_ShouldSwitchAtLocalMidnight(int utcHour, int utcMinute, int expectedDay)
    {
        var clock = new BusinessClock(TimeProvider.System);

        var date = clock.DateOf(new DateTimeOffset(2026, 10, 3, utcHour, utcMinute, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, expectedDay), date);
    }
}
