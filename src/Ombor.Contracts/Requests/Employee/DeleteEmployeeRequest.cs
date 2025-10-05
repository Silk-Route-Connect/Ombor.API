namespace Ombor.Contracts.Requests.Employee;

/// <summary>
/// Request to delete an employee by ID.
/// </summary>
/// <param name="Id">ID of the employee to delete.</param>
public sealed record DeleteEmployeeRequest(int Id);
