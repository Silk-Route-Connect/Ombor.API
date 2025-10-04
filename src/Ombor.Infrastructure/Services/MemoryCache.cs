using Microsoft.Extensions.Caching.Memory;
using Ombor.Application.Interfaces;

namespace Ombor.Infrastructure.Services;

internal sealed class MemoryCache(IMemoryCache cache) : IRedisService
{
    public Task<T?> GetAsync<T>(string key)
        => Task.FromResult(cache.Get<T>(key));

    public Task RemoveAsync(string key)
    {
        cache.Remove(key);

        return Task.CompletedTask;
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        cache.GetOrCreateAsync(key, entry =>
        {
            if (expiry.HasValue)
            {
                entry.SetAbsoluteExpiration(expiry.Value);
            }

            return Task.FromResult(value);
        });

        return Task.FromResult(true);
    }
}
