using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class Organization : AuditableEntity
{
    public required string Name { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Business profile (Settings). Optional free-text contact/address fields.</summary>
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Hosted URL of the organization logo, set via the Settings logo upload.</summary>
    public string? LogoUrl { get; set; }

    public virtual ICollection<User> Users { get; set; }
    public virtual ICollection<Role> Roles { get; set; }

    public Organization()
    {
        Users = new HashSet<User>();
        Roles = new HashSet<Role>();
    }
}
