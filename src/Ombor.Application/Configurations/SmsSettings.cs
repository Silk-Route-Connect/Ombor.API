using System.ComponentModel.DataAnnotations;

namespace Ombor.Application.Configurations;

/// <summary>
/// Eskiz SMS provider settings. Authenticate with <see cref="Email"/> + <see cref="Password"/> (the token is obtained
/// from <see cref="AuthUrl"/>, cached, and renewed before it expires or when Eskiz answers 401), or with a fixed
/// <see cref="Token"/> — which expires 30 days after Eskiz issues it and is used as the fallback when the account
/// login fails. At least one of the two must be configured. Keep all three secrets out of tracked appsettings files.
/// </summary>
public sealed class SmsSettings : IValidatableObject
{
    public const string SectionName = nameof(SmsSettings);

    public const string DefaultAuthUrl = "https://notify.eskiz.uz/api/auth/login";

    /// <summary>A fixed Eskiz bearer token (fallback; expires 30 days after issue).</summary>
    public string? Token { get; init; }

    /// <summary>Eskiz account email; with <see cref="Password"/> enables automatic token renewal.</summary>
    public string? Email { get; init; }

    /// <summary>Eskiz account password; with <see cref="Email"/> enables automatic token renewal.</summary>
    public string? Password { get; init; }

    /// <summary>Eskiz login endpoint that exchanges <see cref="Email"/> + <see cref="Password"/> for a token.</summary>
    public string AuthUrl { get; init; } = DefaultAuthUrl;

    [Required(ErrorMessage = "ApiUrl is required.")]
    public required string ApiUrl { get; set; }

    [Required(ErrorMessage = "From Number is required.")]
    public required string FromNumber { get; init; }

    /// <summary>
    /// In the Development environment SMS are written to the log instead of sent; set this to <c>true</c> to send
    /// real SMS from a local run. Ignored in every other environment.
    /// </summary>
    public bool SendInDevelopment { get; init; }

    /// <summary>Whether the account credentials are configured, so a token can be (re)issued automatically.</summary>
    public bool HasAccountCredentials => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HasAccountCredentials && string.IsNullOrWhiteSpace(Token))
        {
            yield return new ValidationResult(
                "SMS needs either an account (Email + Password) or a Token.",
                [nameof(Token), nameof(Email), nameof(Password)]);
        }
    }
}
