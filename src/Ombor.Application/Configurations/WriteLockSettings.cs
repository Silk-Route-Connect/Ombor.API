using System.ComponentModel.DataAnnotations;

namespace Ombor.Application.Configurations;

/// <summary>
/// The organization write lock every money or stock write takes (<see cref="Interfaces.IOrganizationWriteLock"/>).
/// The section is optional; the default fits small-shop volume, where a write holds the lock for well under a second.
/// </summary>
public sealed class WriteLockSettings
{
    public const string SectionName = nameof(WriteLockSettings);

    /// <summary>
    /// How long a write waits for the organization's lock before it is refused with 409 <c>conflict.busy</c>. Kept
    /// below the 30-second database command timeout so the wait ends as a clean 409, never a SQL timeout 500.
    /// </summary>
    [Range(1, 25)]
    public int TimeoutSeconds { get; init; } = 10;
}
