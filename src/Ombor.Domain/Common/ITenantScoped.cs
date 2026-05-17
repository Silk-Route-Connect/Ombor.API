namespace Ombor.Domain.Common;

/// <summary>
/// Marks an entity as belonging to exactly one tenant. <see cref="TenantId"/> is
/// stamped automatically on insert and enforced by an EF Core global query filter —
/// application code must never set or filter on it manually.
/// </summary>
public interface ITenantScoped
{
    int TenantId { get; set; }
}
