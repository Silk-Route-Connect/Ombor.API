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
public sealed class GetProductsRequest : PagedRequest
{
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ProductType? Type { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "name_asc";

    public GetProductsRequest() { }

    public GetProductsRequest(
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        ProductType? type = null,
        string? searchTerm = null,
        string? sortBy = "name_asc",
        int pageNumber = 1,
        int pageSize = 10) : base(pageNumber, pageSize)
    {
        CategoryId = categoryId;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        Type = type;
        SearchTerm = searchTerm;
        SortBy = sortBy;
    }
}
