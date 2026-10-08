using Microsoft.EntityFrameworkCore.Storage;

namespace Ombor.Application.Interfaces;

/// <summary>
/// The one way a money or stock write starts (backend-7): it opens the write's database transaction and takes the
/// organization's exclusive write lock inside it, so the writes of one organization run one at a time. Everything the
/// write checks — stock on hand, wallet balance, remaining debt, refund caps, an order's status — is read after the
/// lock is held, so no parallel request can change those figures between the check and the write. Other
/// organizations are never blocked, and reads take no lock.
/// </summary>
public interface IOrganizationWriteLock
{
    /// <summary>
    /// Begins the transaction and waits for the current organization's write lock. The lock is released when the
    /// transaction commits or rolls back; disposing the returned transaction without committing rolls it back.
    /// </summary>
    /// <exception cref="Domain.Exceptions.ConflictException">
    /// <c>conflict.busy</c> (409) when the lock is not free within <see cref="Configurations.WriteLockSettings.TimeoutSeconds"/>.
    /// </exception>
    Task<IDbContextTransaction> BeginOrgWriteAsync(CancellationToken cancellationToken = default);
}
