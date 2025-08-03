using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Employee;

public sealed record UpdateEmployeeResponse(
    int Id,
    string FullName,
    decimal Salary,
    string PhoneNumber,
    string Email,
    string Address,
    string? Description,
    EmployeeRole Role,
    EmployeeAccess Access,
    EmployeeStatus Status,
    DateTime DateOfEmployment);