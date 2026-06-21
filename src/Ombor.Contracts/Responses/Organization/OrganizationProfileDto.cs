namespace Ombor.Contracts.Responses.Organization;

/// <summary>The organization's business profile (Settings). All fields except name are optional.</summary>
/// <param name="Name">The business name.</param>
/// <param name="Address">The business address.</param>
/// <param name="Phone">The business phone.</param>
/// <param name="Email">The business email.</param>
/// <param name="LogoUrl">Hosted URL of the logo, or null if none uploaded.</param>
public sealed record OrganizationProfileDto(
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    string? LogoUrl);
