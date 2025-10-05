using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Category;

/// <summary>
/// Request to retrieve a filtered list of categories.
/// </summary>
/// <param name="SearchTerm">
/// Optional case‐insensitive term to filter by name or description.
/// </param>
public sealed record GetCategoriesRequest(
    string? SearchTerm = null,
    string? SortBy = "name_asc"
) : PagedRequest
{
    public GetCategoriesRequest(
        string? searchTerm,
        string? sortBy,
        int pageNumber,
        int pageSize)
        : this(searchTerm, sortBy)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
