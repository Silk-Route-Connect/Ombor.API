using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;

namespace Ombor.Application.Services;

internal sealed class OrganizationService(IApplicationDbContext context) : IOrganizationService
{
    public async Task<Organization> EnsureOrganizationExistsAsync(string organizationName)
    {
        var existingOrganization = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name == organizationName);

        if (existingOrganization is not null)
        {
            return existingOrganization;
        }

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
