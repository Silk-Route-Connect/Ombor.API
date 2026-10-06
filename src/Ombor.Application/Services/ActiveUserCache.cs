using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;

namespace Ombor.Application.Services;

internal sealed class ActiveUserCache(IApplicationDbContext context, IRedisService redisService) : IActiveUserCache
{
    private const string KeyPattern = "user:active:{0}";

    // Bounds how long a deactivation takes to reach other instances (no cross-instance invalidation exists yet).
    private static readonly TimeSpan CacheWindow = TimeSpan.FromSeconds(30);

    public async Task<bool> IsActiveAsync(int userId, CancellationToken cancellationToken = default)
    {
        var key = string.Format(KeyPattern, userId);
        var cached = await redisService.GetAsync<bool?>(key);

        if (cached.HasValue)
        {
            return cached.Value;
        }

        // Runs while the bearer token is being validated, before any organization context exists — bypass the
        // org filter and look the user up by its own id. A user that no longer exists counts as inactive.
        var isActive = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (bool?)u.IsActive)
            .FirstOrDefaultAsync(cancellationToken) ?? false;

        await redisService.SetAsync<bool?>(key, isActive, CacheWindow);

        return isActive;
    }

    public Task InvalidateAsync(int userId) =>
        redisService.RemoveAsync(string.Format(KeyPattern, userId));
}
