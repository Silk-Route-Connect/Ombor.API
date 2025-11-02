using Microsoft.AspNetCore.Http;
using Ombor.Application.Interfaces;

namespace Ombor.Infrastructure.Services;

internal class TenantProvider(IHttpContextAccessor httpContext) : ITenantProvider
{
    public int GetCurrentTenantId()
    {
        var claim = httpContext.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == "organizationId")?.Value;

        if (string.IsNullOrEmpty(claim))
            throw new Exception("Tenant (organizationId) claim not found in current user context.");

        if (!int.TryParse(claim, out var organizationId))
            throw new Exception("Invalid tenant (organizationId) format.");

        if (organizationId == 0)
            throw new Exception("Tenant (organizationId) cannot be 0.");

        return organizationId;
    }
}
