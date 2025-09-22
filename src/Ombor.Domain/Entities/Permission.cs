using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class Permission : EntityBase
{
    public required string Module { get; set; }
    public required string Action { get; set; }
    public string? Resource { get; set; }
    public string? Description { get; set; }
    public virtual ICollection<Role> Roles { get; set; }

    public Permission()
    {
        Roles = new HashSet<Role>();
    }
}
