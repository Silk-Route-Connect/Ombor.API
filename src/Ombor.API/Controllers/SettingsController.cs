using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Organization;
using Ombor.Contracts.Responses.Organization;

namespace Ombor.API.Controllers;

/// <summary>
/// Settings — the organization business profile and user management for the current organization.
/// </summary>
[ApiController]
[Route("api/settings")]
public sealed class SettingsController(IOrganizationService organizationService) : ControllerBase
{
    /// <summary>Returns the current organization's business profile.</summary>
    [HttpGet("organization")]
    [ProducesResponseType(typeof(OrganizationProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationProfileDto>> GetOrganizationAsync()
    {
        var response = await organizationService.GetProfileAsync();

        return Ok(response);
    }

    /// <summary>Updates the current organization's business profile (multipart; optional logo upload).</summary>
    [HttpPut("organization")]
    [ProducesResponseType(typeof(OrganizationProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrganizationProfileDto>> UpdateOrganizationAsync([FromForm] UpdateOrganizationRequest request)
    {
        var response = await organizationService.UpdateProfileAsync(request);

        return Ok(response);
    }
}
