namespace Ombor.Tests.Common.Helpers;

/// <summary>A clock frozen at one instant, so calendar logic (the business day, ages, buckets) is deterministic.</summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
