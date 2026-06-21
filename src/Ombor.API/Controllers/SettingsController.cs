using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Organization;
using Ombor.Contracts.Requests.User;
using Ombor.Contracts.Responses.Organization;
using Ombor.Contracts.Responses.User;

namespace Ombor.API.Controllers;

/// <summary>
/// Settings — the organization business profile and user management for the current organization.
/// </summary>
[ApiController]
[Route("api/settings")]
public sealed class SettingsController(
    IOrganizationService organizationService,
    IUserService userService) : ControllerBase
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

    /// <summary>Lists the organization's users, including deactivated ones.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(TenantUserDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantUserDto[]>> GetUsersAsync()
    {
        var response = await userService.GetUsersAsync();

        return Ok(response);
    }

    /// <summary>Invites a user (by phone) to the current organization.</summary>
    [HttpPost("users/invite")]
    [ProducesResponseType(typeof(TenantUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TenantUserDto>> InviteUserAsync([FromBody] InviteUserRequest request)
    {
        var response = await userService.InviteAsync(request);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Deactivates a user (rule 41 — never deleted). The current user cannot deactivate themselves.</summary>
    [HttpPost("users/{id:int:min(1)}/deactivate")]
    [ProducesResponseType(typeof(TenantUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantUserDto>> DeactivateUserAsync([FromRoute] int id)
    {
        var response = await userService.DeactivateAsync(id);

        return Ok(response);
    }

    /// <summary>Reactivates a previously deactivated user.</summary>
    [HttpPost("users/{id:int:min(1)}/reactivate")]
    [ProducesResponseType(typeof(TenantUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantUserDto>> ReactivateUserAsync([FromRoute] int id)
    {
        var response = await userService.ReactivateAsync(id);

        return Ok(response);
    }

    /// <summary>Sets the current user's interface language (applies only to them).</summary>
    [HttpPut("language")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetLanguageAsync([FromBody] SetLanguageRequest request)
    {
        await userService.SetLanguageAsync(request);

        return NoContent();
    }
}
