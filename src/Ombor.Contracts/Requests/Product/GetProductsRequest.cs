using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Product;

/// <summary>
/// Request to retrieve a list of products with optional filtering.
/// </summary>
/// <param name="SearchTerm">
///   Optional case‑insensitive term to filter by Name, SKU, Description, or Barcode.
/// </param>
/// <param name="CategoryId">Optional category filter (must be &gt; 0).</param>
/// <param name="MinPrice">Optional minimum sale price filter.</param>
/// <param name="MaxPrice">Optional maximum sale price filter.</param>
public sealed record GetProductsRequest(
    int? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    ProductType? Type = null,
    string? SearchTerm = null,
    string? SortBy = "name_asc") : PagedRequest
{
    public GetProductsRequest(
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        ProductType? type,
        string? searchTerm,
        string? sortBy,
        int pageNumber,
        int pageSize) : this(categoryId, minPrice, maxPrice, type, searchTerm, sortBy)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
