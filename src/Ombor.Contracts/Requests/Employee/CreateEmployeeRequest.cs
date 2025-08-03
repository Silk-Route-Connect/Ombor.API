using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Employee;

public sealed record CreateEmployeeRequest(
    string FullName,
    decimal Salary,
    string PhoneNumber,
    string Email,
    string? Address,
    string? Description,
    EmployeeRole Role,
    EmployeeAccess Access,
    DateTime DateOfEmployment
    );
