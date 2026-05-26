using Ombor.Contracts.Enums;

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
/// <param name="IsArchived">When true, returns archived products only; otherwise active products only.</param>
public sealed record GetProductsRequest(
    string? SearchTerm,
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    ProductType? Type,
    bool? IsArchived = null);
