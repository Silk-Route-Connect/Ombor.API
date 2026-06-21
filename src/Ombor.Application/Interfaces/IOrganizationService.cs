using Ombor.Contracts.Requests.Organization;
using Ombor.Contracts.Responses.Organization;
using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface IOrganizationService
{
    Task<Organization> CreateAsync(string organizationName);

    /// <summary>The current organization's business profile (Settings).</summary>
    Task<OrganizationProfileDto> GetProfileAsync();

    /// <summary>Updates the current organization's business profile; uploads a new logo when provided.</summary>
    Task<OrganizationProfileDto> UpdateProfileAsync(UpdateOrganizationRequest request);
}
