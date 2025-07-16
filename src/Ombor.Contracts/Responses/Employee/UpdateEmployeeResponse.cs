namespace Ombor.Contracts.Responses.Employee;

public sealed record UpdateEmployeeResponse(
    int Id,
    string FullName,
    string Role,
    bool IsActive);