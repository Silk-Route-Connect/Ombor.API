namespace Ombor.Contracts.Responses.Employee;

public sealed record UpdateEmployeeResponse(
    int Id,
    string FullName,
    decimal Salary,
    string PhoneNumber,
    string Email,
    string Address,
    string? Description,
    string Role,
    string Access,
    string Status,
    DateOnly DateOfEmployment);