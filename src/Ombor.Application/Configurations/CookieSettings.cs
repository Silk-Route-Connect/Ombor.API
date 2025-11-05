using System.ComponentModel.DataAnnotations;

namespace Ombor.Application.Configurations;

public class CookieSettings
{
    public const string SectionName = nameof(CookieSettings);

    /// <summary>
    /// Cookie domain. Use ".miraziz.net" for production to share across subdomains.
    /// Use null for development to let it default to the request host.
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// Whether to require HTTPS. Should be true in production, false in development.
    /// </summary>
    public bool Secure { get; init; }

    /// <summary>
    /// SameSite policy. Use "None" for production (cross-subdomain), "Lax" for development.
    /// </summary>
    [Required(ErrorMessage = "Same site is required")]
    public required string SameSite { get; init; }
}
