namespace Ombor.Application.Interfaces;

/// <summary>
/// Answers «may this user still use the API?» for every request that carries an access token (rule 41), cached
/// briefly so the check costs one query per user per cache window. Deactivate/reactivate invalidate the entry, so
/// on a single instance the change applies to the very next request.
/// </summary>
public interface IActiveUserCache
{
    Task<bool> IsActiveAsync(int userId, CancellationToken cancellationToken = default);

    Task InvalidateAsync(int userId);
}
