using Ombor.Domain.Common;
using Ombor.Domain.Enums;

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
    /// Gets or sets the position associated with employee.
    /// </summary>
    public required string Position { get; set; }

    /// <summary>
    /// Gets or sets the salary of the employee.
    /// </summary>
    public required decimal Salary { get; set; }

    /// <summary>
    /// Gets or sets the current employment status of the employee.
    /// </summary>
    public required EmployeeStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date of employement of the employee
    /// </summary>
    public required DateOnly DateOfEmployment { get; set; }

    /// <summary>
    /// Gets or sets the contact info of the employee.
    /// </summary>
    public ContactInfo? ContactInfo { get; set; }

    public virtual ICollection<Payment> Payrolls { get; set; } = [];
}
