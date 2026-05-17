using Microsoft.AspNetCore.Http;
using Ombor.Application.Interfaces;

namespace Ombor.Infrastructure.Services;

internal sealed class HttpContextTenantAccessor(IHttpContextAccessor httpContextAccessor) : ITenantAccessor
{
    public const string TenantClaimType = "tenant_id";

    private int? _override;

    public int? TenantId => _override ?? ResolveFromClaims();

    public void SetTenant(int tenantId) => _override = tenantId;

    private int? ResolveFromClaims()
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirst(TenantClaimType)?.Value;

        return int.TryParse(value, out var tenantId) ? tenantId : null;
    }
}
