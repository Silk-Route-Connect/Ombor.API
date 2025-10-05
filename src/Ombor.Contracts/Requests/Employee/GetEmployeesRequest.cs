namespace Ombor.Contracts.Requests.Employee;

/// <summary>
/// Request to retrieve employees, optionally filtered by a search term.
/// </summary>
/// <param name="SearchTerm">Search term to apply for filtering employees.</param>
public sealed record GetEmployeesRequest(string? SearchTerm = null);
