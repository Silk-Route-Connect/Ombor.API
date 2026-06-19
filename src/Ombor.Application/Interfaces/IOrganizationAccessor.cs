namespace Ombor.Application.Interfaces;

/// <summary>
/// Resolves the organization the current request belongs to. Returns <c>null</c> when there
/// is no organization context — e.g. database seeding, background work, or design-time tooling.
/// </summary>
public interface IOrganizationAccessor
{
    int? OrganizationId { get; }

    /// <summary>
    /// Pins the organization for a non-request context (database seeding, background work).
    /// Outside an HTTP request this is the only way a organization becomes available.
    /// </summary>
    void SetOrganization(int organizationId);
}
