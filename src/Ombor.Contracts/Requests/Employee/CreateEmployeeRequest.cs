using Ombor.Contracts.Common;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Employee;

/// <summary>
/// Request to create a new employee.
/// </summary>
/// <param name="Name">Full name of the employee.</param>
/// <param name="Position">Position of the employee.</param>
/// <param name="Salary">Salary of the employee</param>
/// <param name="Status">Current status of the employee.</param>
/// <param name="DateOfEmployment">Date of employment of the employee.</param>
/// <param name="ContactInfo">Contact info of the employee.</param>
public sealed record CreateEmployeeRequest(
    string Name,
    string Position,
    decimal Salary,
    EmployeeStatus Status,
    DateOnly DateOfEmployment,
    ContactInfo? ContactInfo);
