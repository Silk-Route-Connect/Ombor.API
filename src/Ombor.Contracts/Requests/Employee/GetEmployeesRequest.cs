using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Employee;

/// <summary>
/// Request to retrieve employees, optionally filtered by a search term.
/// </summary>
/// <param name="SearchTerm">Search term to apply for filtering employees.</param>
public sealed record GetEmployeesRequest(
    EmployeeStatus? Status = null,
    string? SearchTerm = null,
    string? SortBy = "hiredate_desc",
    int PageNumber = 1,
    int PageSize = 10) : PagedRequest(PageNumber, PageSize);
