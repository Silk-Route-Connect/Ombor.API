namespace Ombor.Application.Interfaces;

/// <summary>
/// Resolves the tenant the current request belongs to. Returns <c>null</c> when there
/// is no tenant context — e.g. database seeding, background work, or design-time tooling.
/// </summary>
public interface ITenantAccessor
{
    int? TenantId { get; }

    /// <summary>
    /// Pins the tenant for a non-request context (database seeding, background work).
    /// Outside an HTTP request this is the only way a tenant becomes available.
    /// </summary>
    void SetTenant(int tenantId);
}
