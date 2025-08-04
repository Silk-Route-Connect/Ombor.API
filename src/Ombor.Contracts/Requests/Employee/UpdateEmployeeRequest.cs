using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Employee;

public sealed record UpdateEmployeeRequest(
    int Id,
    string FullName,
    decimal Salary,
    string PhoneNumber,
    string Email,
    string? Address,
    string? Description,
    EmployeeRole Role,
    EmployeeAccess Access,
    EmployeeStatus Status,
    DateOnly DateOfEmployment
    );
