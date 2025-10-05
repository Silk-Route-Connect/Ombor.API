using Ombor.Contracts.Common;

namespace Ombor.Contracts.Responses.Employee;

/// <summary>
/// Response returned after creating a new employee.
/// </summary>
/// <param name="Id">ID of the newly created employee.</param>
/// <param name="FullName">Full name of the employee.</param>
/// <param name="Position">Position of the employee.</param>
/// <param name="Status">Current status of the employee.</param>
/// <param name="Salary">Salary amount of the employee.</param>
/// <param name="DateOfEmployment">Date of employement of the employee.</param>
/// <param name="ContactInfo">Contact info of the employee.</param>
public sealed record CreateEmployeeResponse(
    int Id,
    string FullName,
    string Position,
    string Status,
    decimal Salary,
    DateOnly DateOfEmployment,
    ContactInfo? ContactInfo);