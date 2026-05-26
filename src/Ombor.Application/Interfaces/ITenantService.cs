using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface ITenantService
{
    Task<Tenant> CreateAsync(string tenantName);
}
