using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class User : AuditableEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public required string PhoneNumber { get; set; }
    public string? TelegramAccount { get; set; }
    public string? Email { get; set; }
    public bool IsPhoneNumberConfirmed { get; set; }

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
