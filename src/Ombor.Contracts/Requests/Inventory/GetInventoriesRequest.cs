using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Inventory;

/// <summary>
/// Request to retrieve a list of inventories with optional filtering.
/// </summary>
/// <param name="SearchTerm">
/// Optional case-insensitive term to filter by Name, Location.
/// </param>
public sealed record GetInventoriesRequest(
    string? SearchTerm = null,
    string? SortBy = "name_asc",
    int PageNumber = 1,
    int PageSize = 10) : PagedRequest(PageNumber, PageSize);