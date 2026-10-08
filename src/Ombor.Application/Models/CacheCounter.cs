namespace Ombor.Application.Models;

/// <summary>The value of an atomic cache counter after an increment, and when its window ends.</summary>
public sealed record CacheCounter(long Count, DateTimeOffset ExpiresAt)
{
    public TimeSpan RemainingWindow =>
        ExpiresAt > DateTimeOffset.UtcNow ? ExpiresAt - DateTimeOffset.UtcNow : TimeSpan.Zero;
}
