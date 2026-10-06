using Microsoft.Extensions.Caching.Memory;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;

namespace Ombor.Infrastructure.Services;

// In-process stand-in for Redis: state is lost on restart and not shared between instances (single instance only).
internal sealed class MemoryCache(IMemoryCache cache) : IRedisService
{
    private readonly object _counterLock = new();

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

    public Task<CacheCounter> IncrementAsync(string key, TimeSpan window)
    {
        lock (_counterLock)
        {
            var now = DateTimeOffset.UtcNow;

            if (!cache.TryGetValue(key, out Counter? counter) || counter is null || counter.ExpiresAt <= now)
            {
                counter = new Counter(now.Add(window));
                cache.Set(key, counter, counter.ExpiresAt);
            }

            counter.Count++;

            return Task.FromResult(new CacheCounter(counter.Count, counter.ExpiresAt));
        }
    }

    private sealed class Counter(DateTimeOffset expiresAt)
    {
        public long Count { get; set; }

        public DateTimeOffset ExpiresAt { get; } = expiresAt;
    }
}
