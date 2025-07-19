namespace Ombor.Contracts.Responses.Employee;

public sealed record EmployeeDto(
    int Id,
    string FullName,
    string Role,
    bool IsActive);