using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;

namespace Ombor.Application.Services;

internal sealed class RefreshTokenStore(
    IApplicationDbContext context,
    IJwtTokenService tokenService,
    IOptions<JwtSettings> jwtSettings) : IRefreshTokenStore
{
    private const int RetentionDays = 30;

    public async Task<string> IssueAsync(User user)
    {
        var token = tokenService.GenerateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            Token = token,
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiresInDays),
            UserId = user.Id,
            User = user
        });

        await context.SaveChangesAsync();

        return token;
    }

    public async Task RevokeAllAsync(int userId, string? keepToken = null)
    {
        var activeTokens = await context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.Token != keepToken)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }
    }

    public async Task PruneAsync(int userId)
    {
        // RefreshToken stores no issue time; it is ExpiresAt − lifetime, so "issued more than RetentionDays ago" is
        // ExpiresAt < now − RetentionDays + lifetime. The revoked/expired guard keeps a live session safe even if
        // the configured lifetime ever exceeds the retention period.
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(jwtSettings.Value.RefreshTokenExpiresInDays - RetentionDays);

        var deadTokens = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.ExpiresAt < cutoff && (t.IsRevoked || t.ExpiresAt <= now))
            .ToListAsync();

        if (deadTokens.Count > 0)
        {
            context.RefreshTokens.RemoveRange(deadTokens);
            await context.SaveChangesAsync();
        }
    }
}
