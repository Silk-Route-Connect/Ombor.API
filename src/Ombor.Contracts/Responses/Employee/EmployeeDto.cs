using Ombor.Contracts.Common;

namespace Ombor.Contracts.Responses.Employee;

/// <summary>
/// A DTO representing an employee in the system.
/// </summary>
/// <param name="Id">The unique identifier of the employee.</param>
/// <param name="Name">Full name of the employee.</param>
/// <param name="Position">Position of the employee.</param>
/// <param name="Status">Status of the employee.</param>
/// <param name="Salary">Salary amount of the employee.</param>
/// <param name="DateOfEmployment">Date of employement of the employee.</param>
/// <param name="ContactInfo">Contact info of the employee.</param>
public sealed record EmployeeDto(
    int Id,
    string Name,
    string Position,
    string Status,
    decimal Salary,
    DateOnly DateOfEmployment,
    ContactInfo? ContactInfo);
