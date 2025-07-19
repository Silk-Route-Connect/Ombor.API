namespace Ombor.Contracts.Requests.Employee;

public sealed record UpdateEmployeeRequest(
    int Id,
    string FullName,
    string Role,
    bool IsActive);
