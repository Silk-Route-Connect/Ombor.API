using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Services;
using Ombor.Domain.Exceptions;
using CacheService = Ombor.Infrastructure.Services.MemoryCache;

namespace Ombor.Tests.Unit.Services;

public sealed class LoginThrottleTests
{
    private const string Phone = "+998901234567";

    private readonly LoginThrottle _throttle = new(
        new CacheService(new MemoryCache(new MemoryCacheOptions())),
        Options.Create(new AuthSecuritySettings()));

    [Fact]
    public async Task BeginAttemptAsync_LocksThePhone_AfterTenFailedAttempts()
    {
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            await _throttle.BeginAttemptAsync(Phone);
        }

        var locked = await Assert.ThrowsAsync<TooManyRequestsException>(() => _throttle.BeginAttemptAsync(Phone));

        Assert.InRange(locked.RetryAfterSeconds, 1, 15 * 60);

        // Other phones are unaffected.
        await _throttle.BeginAttemptAsync("+998901234568");
    }

    [Fact]
    public async Task ResetAsync_ClearsTheCount_AfterASuccessfulPassword()
    {
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            await _throttle.BeginAttemptAsync(Phone);
        }

        await _throttle.ResetAsync(Phone);

        await _throttle.BeginAttemptAsync(Phone);
    }
}
