using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

/// <summary>Issues, revokes and prunes a user's refresh tokens (one row per session).</summary>
public interface IRefreshTokenStore
{
    /// <summary>Creates and saves a new refresh token for <paramref name="user"/> and returns its value.</summary>
    Task<string> IssueAsync(User user);

    /// <summary>
    /// Marks every active refresh token of the user revoked, except <paramref name="keepToken"/> (the caller's own
    /// session) when given. Changes are tracked only: the caller's <c>SaveChangesAsync</c> commits them together
    /// with its own change, so the two can never apply partially.
    /// </summary>
    Task RevokeAllAsync(int userId, string? keepToken = null);

    /// <summary>Deletes the user's revoked or expired tokens issued more than 30 days ago.</summary>
    Task PruneAsync(int userId);
}
