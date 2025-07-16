using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class Employee : AuditableEntity
{
    public required string FullName { get; set; }
    public required string Role { get; set; }
    public bool IsActive { get; set; }
}
