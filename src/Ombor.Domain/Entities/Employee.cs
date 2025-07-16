using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents an employee in the system.
/// </summary>
public class Employee : AuditableEntity
{
    /// <summary>
    /// Gets or sets the full name of the employee.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Gets or sets the role associated with employee.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the employee is active.
    /// </summary>
    public bool IsActive { get; set; }
}
