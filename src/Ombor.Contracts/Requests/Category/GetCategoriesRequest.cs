using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Category;

/// <summary>
/// Request to retrieve a filtered list of categories.
/// </summary>
/// <param name="SearchTerm">
/// Optional case‐insensitive term to filter by name or description.
/// </param>
public sealed class GetCategoriesRequest : PagedRequest
{
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "name_asc";

    public GetCategoriesRequest() { }

    public GetCategoriesRequest(
        string? searchTerm = null,
        string? sortBy = "name_asc",
        int pageNumber = 1,
        int pageSize = 10) : base(pageNumber, pageSize)
    {
        SearchTerm = searchTerm;
        SortBy = sortBy;
    }
}
