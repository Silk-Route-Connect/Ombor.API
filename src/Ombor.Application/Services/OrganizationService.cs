using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Contracts.Requests.Organization;
using Ombor.Contracts.Responses.Organization;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class OrganizationService(
    IApplicationDbContext context,
    IOrganizationAccessor organizationAccessor,
    IRequestValidator validator,
    IFileService fileService) : IOrganizationService
{
    private const string LogoSubfolder = "organizations";

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

    public async Task<OrganizationProfileDto> GetProfileAsync()
    {
        var organization = await GetCurrentOrganizationAsync();

        return ToDto(organization);
    }

    public async Task<OrganizationProfileDto> UpdateProfileAsync(UpdateOrganizationRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var organization = await GetCurrentOrganizationAsync();

        organization.Name = request.Name;
        organization.Address = request.Address;
        organization.Phone = request.Phone;
        organization.Email = request.Email;

        // A new file replaces the logo; omitting it keeps the existing one.
        if (request.Logo is not null)
        {
            var upload = await fileService.UploadAsync(request.Logo, LogoSubfolder);
            organization.LogoUrl = upload.Url;
        }

        await context.SaveChangesAsync();

        return ToDto(organization);
    }

    private async Task<Organization> GetCurrentOrganizationAsync()
    {
        var organizationId = organizationAccessor.OrganizationId
            ?? throw new InvalidOperationException("No organization in the current context.");

        return await context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId)
            ?? throw new EntityNotFoundException<Organization>(organizationId);
    }

    private static OrganizationProfileDto ToDto(Organization organization) =>
        new(organization.Name, organization.Address, organization.Phone, organization.Email, organization.LogoUrl);
}
