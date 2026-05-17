using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;

namespace Ombor.Application.Services;

internal sealed class TenantService(IApplicationDbContext context) : ITenantService
{
    public async Task<Tenant> CreateAsync(string tenantName)
    {
        var newTenant = new Tenant
        {
            Name = tenantName,
            IsActive = true
        };

        context.Tenants.Add(newTenant);
        await context.SaveChangesAsync();

        return newTenant;
    }
}
