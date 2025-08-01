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
    /// Gets or sets the role associated with employee.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Gets or sets the salary of the employee.
    /// </summary>
    public decimal Salary { get; set; }

    /// <summary>
    /// Gets or sets the phone number of the employee.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the email address of the employee.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the address of the employee.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the description of the employee.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the current employment status of the employee.
    /// </summary>
    public EmployeeStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the access level assigned to the employee.
    /// </summary>
    public EmployeeAccess Access { get; set; }

    /// <summary>
    /// Gets or sets the date of employement of the employee
    /// </summary>
    public DateTime DateOfEmployment { get; set; }
}
