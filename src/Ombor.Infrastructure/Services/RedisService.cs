using System.Text.Json;
using Ombor.Application.Interfaces;
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
}
