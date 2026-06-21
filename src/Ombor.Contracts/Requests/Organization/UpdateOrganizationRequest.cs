using Microsoft.AspNetCore.Http;

namespace Ombor.Contracts.Requests.Organization;

/// <summary>
/// Updates the organization's business profile (Settings). Sent as multipart/form-data so the
/// optional <see cref="Logo"/> image can be uploaded alongside the text fields.
/// </summary>
/// <param name="Name">The business name (required).</param>
/// <param name="Address">The business address.</param>
/// <param name="Phone">The business phone.</param>
/// <param name="Email">The business email.</param>
/// <param name="Logo">Optional new logo image; when omitted the existing logo is kept.</param>
public sealed record UpdateOrganizationRequest(
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    IFormFile? Logo);
