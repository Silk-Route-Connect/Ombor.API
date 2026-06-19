namespace Ombor.Domain.Common;

/// <summary>
/// Marks an entity as belonging to exactly one organization. <see cref="OrganizationId"/> is
/// stamped automatically on insert and enforced by an EF Core global query filter —
/// application code must never set or filter on it manually.
/// </summary>
public interface IOrganizationScoped
{
    int OrganizationId { get; set; }
}
