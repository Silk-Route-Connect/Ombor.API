using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;

namespace Ombor.Application.Services;

internal sealed class OrganizationService(IApplicationDbContext context) : IOrganizationService
{
    public async Task<Organization> CreateAsync(string organizationName)
    {
        var newOrganization = new Organization
        {
            Name = organizationName,
            IsActive = true
        };

        context.Organizations.Add(newOrganization);
        await context.SaveChangesAsync();

        return newOrganization;
    }
}
