namespace Ombor.Contracts.Responses.Employee;

public sealed record CreateEmployeeResponse(
    int Id,
    string FullName,
    string Role,
    bool IsActive);