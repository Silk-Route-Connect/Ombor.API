using System.ComponentModel.DataAnnotations;

namespace Ombor.Application.Configurations;

public sealed class JwtSettings
{
    public const string SectionName = nameof(JwtSettings);

    [Required]
    [MinLength(32)]
    public required string Key { get; init; }

    [Required]
    [MinLength(1)]
    public required string Issuer { get; init; }

    [Required]
    [MinLength(1)]
    public required string Audience { get; init; }

    [Range(1, 24, ErrorMessage = "Access token expiration must be between 1 hour and 24 hours")]
    public int AccessTokenExpiresInHours { get; init; }

    [Range(1, 30, ErrorMessage = "Refresh token expiration must be between 1 day and 30 days")]
    public int RefreshTokenExpiresInDays { get; init; }
}
