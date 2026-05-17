using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class Tenant : AuditableEntity
{
    public required string Name { get; set; }
    public bool IsActive { get; set; }

    public virtual ICollection<User> Users { get; set; }
    public virtual ICollection<Role> Roles { get; set; }

    public Tenant()
    {
        Users = new HashSet<User>();
        Roles = new HashSet<Role>();
    }
}
