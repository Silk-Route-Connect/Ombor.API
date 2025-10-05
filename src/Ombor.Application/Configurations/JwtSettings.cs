using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Ombor.Application.Configurations;

public sealed class JwtSettings
{
    public const string SectionName = nameof(JwtSettings);

    public SymmetricSecurityKey SecurityKey => new(Encoding.UTF8.GetBytes(Key));

    [Required(ErrorMessage = "JWT secret key is required.")]
    [MinLength(32, ErrorMessage = "JWT secret key must contain at least 32 characters.")]
    public required string Key { get; init; }

    [Required(ErrorMessage = "JWT issuer is required.")]
    public required string Issuer { get; init; }

    [Required(ErrorMessage = "JWT audience is required.")]
    public required string Audience { get; init; }

    [Range(1, 100, ErrorMessage = "Access token expiration must be between 1 hour and 100 hours")]
    public required int AccessTokenExpiresInHours { get; init; }

    [Range(1, 30, ErrorMessage = "Refresh token expiration must be between 1 day and 30 days")]
    public required int RefreshTokenExpiresInDays { get; init; }
}
