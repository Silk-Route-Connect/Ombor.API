using Ombor.Contracts.Common;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Employee;

/// <summary>
/// Request to update an existing employee.
/// </summary>
/// <param name="Id">The ID of the employee to update.</param>
/// <param name="FullName">Updated full name of the employee.</param>
/// <param name="Position">Updated position of the employee.</param>
/// <param name="Salary">Updated salary amount of the empoyee.</param>
/// <param name="Status">Updated status of the employee.</param>
/// <param name="DateOfEmployment">Updated date of employement of the employee.</param>
/// <param name="ContactInfo">Updated contact info of the employee.</param>
public sealed record UpdateEmployeeRequest(
    int Id,
    string FullName,
    string Position,
    decimal Salary,
    EmployeeStatus Status,
    DateOnly DateOfEmployment,
    ContactInfo? ContactInfo);
