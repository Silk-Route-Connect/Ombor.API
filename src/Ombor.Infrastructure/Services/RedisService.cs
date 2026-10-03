using System.Text.Json;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using StackExchange.Redis;

namespace Ombor.Infrastructure.Services;

internal sealed class RedisService(IConnectionMultiplexer connection) : IRedisService
{
    private readonly IDatabase _redis = connection.GetDatabase();

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);

        return await _redis.StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _redis.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task RemoveAsync(string key)
        => await _redis.KeyDeleteAsync(key);

    public async Task<CacheCounter> IncrementAsync(string key, TimeSpan window)
    {
        var count = await _redis.StringIncrementAsync(key);

        if (count == 1)
        {
            await _redis.KeyExpireAsync(key, window);
        }

        var ttl = await _redis.KeyTimeToLiveAsync(key) ?? window;

        return new CacheCounter(count, DateTimeOffset.UtcNow.Add(ttl));
    }
}
