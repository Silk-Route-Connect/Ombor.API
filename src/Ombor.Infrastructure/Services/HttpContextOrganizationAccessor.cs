using Microsoft.AspNetCore.Http;
using Ombor.Application.Interfaces;

namespace Ombor.Infrastructure.Services;

internal sealed class HttpContextOrganizationAccessor(IHttpContextAccessor httpContextAccessor) : IOrganizationAccessor
{
    public const string OrganizationClaimType = "organization_id";

    private int? _override;

    public int? OrganizationId => _override ?? ResolveFromClaims();

    public void SetOrganization(int organizationId) => _override = organizationId;

    private int? ResolveFromClaims()
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirst(OrganizationClaimType)?.Value;

        return int.TryParse(value, out var organizationId) ? organizationId : null;
    }
}
