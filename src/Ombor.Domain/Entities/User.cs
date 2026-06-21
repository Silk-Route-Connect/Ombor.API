using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class User : AuditableEntity, IOrganizationScoped
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public required string PhoneNumber { get; set; }
    public string? TelegramAccount { get; set; }
    public string? Email { get; set; }
    public bool IsPhoneNumberConfirmed { get; set; }

    /// <summary>Whether the user can authenticate. Deactivated users (rule 41) remain as audit actors but cannot log in.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the user was deactivated; null while active. Surfaces as the user's last-active date.</summary>
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>The user's interface language (ru / uz-Latn / uz-Cyrl). A per-user preference.</summary>
    public string Language { get; set; } = "ru";

    public int OrganizationId { get; set; }
    public required virtual Organization Organization { get; set; }

    public virtual ICollection<Role> Roles { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; }

    public User()
    {
        Roles = new HashSet<Role>();
        RefreshTokens = new HashSet<RefreshToken>();
    }
}
