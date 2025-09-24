using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class Role : EntityBase
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int OrganizationId { get; set; }
    public required virtual Organization Organization { get; set; }
    public virtual ICollection<User> Users { get; set; }
    public virtual ICollection<Permission> Permissions { get; set; }

    public Role()
    {
        Users = new HashSet<User>();
        Permissions = new HashSet<Permission>();
    }
}
