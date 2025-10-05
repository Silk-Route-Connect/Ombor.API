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
        if (expiry.HasValue)
        {
            cache.Set(key, value, expiry.Value);
        }
        else
        {
            cache.Set(key, value);
        }

        return Task.FromResult(true);
    }
}
