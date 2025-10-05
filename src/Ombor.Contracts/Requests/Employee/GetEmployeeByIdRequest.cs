namespace Ombor.Contracts.Requests.Employee;

/// <summary>
/// Request to retrieve an employee by its identifier.
/// </summary>
/// <param name="Id">The ID of the employee to fetch.</param>
public sealed record GetEmployeeByIdRequest(int Id);
