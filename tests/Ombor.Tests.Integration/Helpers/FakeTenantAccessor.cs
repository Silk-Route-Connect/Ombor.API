using Ombor.Application.Interfaces;

namespace Ombor.Tests.Integration.Helpers;

/// <summary>
/// Test <see cref="ITenantAccessor"/> with a fixed tenant. Integration tests run
/// entirely within a single tenant (id 1, matching <see cref="AuthHandler"/>).
/// </summary>
public sealed class FakeTenantAccessor(int? tenantId = 1) : ITenantAccessor
{
    public int? TenantId { get; private set; } = tenantId;

    public void SetTenant(int tenantId) => TenantId = tenantId;
}
