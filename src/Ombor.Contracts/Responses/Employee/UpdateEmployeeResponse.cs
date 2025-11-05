using Ombor.Contracts.Common;

namespace Ombor.Contracts.Responses.Employee;

/// <summary>
/// A response containing details of an updated employee.
/// </summary>
/// <param name="Id">The unique identifier of the employee.</param>
/// <param name="Name">Full name of the employee.</param>
/// <param name="Position">Position of the employee.</param>
/// <param name="Status">Current status of the employee.</param>
/// <param name="Salary">Salary amount of the employee.</param>
/// <param name="DateOfEmployment">Date of employment of the employee.</param>
/// <param name="ContactInfo">Contact info of the employee.</param>
public sealed record UpdateEmployeeResponse(
    int Id,
    string Name,
    string Position,
    string Status,
    decimal Salary,
    DateOnly DateOfEmployment,
    ContactInfo? ContactInfo);
