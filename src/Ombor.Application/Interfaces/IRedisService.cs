using Ombor.Application.Models;

namespace Ombor.Application.Interfaces;

public interface IRedisService
{
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key);
    Task RemoveAsync(string key);

    /// <summary>
    /// Atomically increments the counter at <paramref name="key"/> and returns its new value. The first increment
    /// creates the counter with a fixed window of <paramref name="window"/>; later increments keep that expiry.
    /// Throttles count with this so parallel requests can never read the same stale count.
    /// </summary>
    Task<CacheCounter> IncrementAsync(string key, TimeSpan window);
}
