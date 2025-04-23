namespace Ombor.Contracts.Requests.Product;

/// <summary>
/// Request model to create a new product.
/// </summary>
/// <param name="CategoryId">The identifier of the category to which this product belongs. Must be &gt; 0.</param>
/// <param name="Name">The product’s name (required).</param>
/// <param name="SKU">The stock‑keeping unit identifier (required, unique).</param>
/// <param name="Measurement">The unit of measurement (e.g. “Piece”, “Kilogram”).</param>
/// <param name="Description">An optional textual description of the product.</param>
/// <param name="Barcode">An optional barcode string.</param>
/// <param name="SalePrice">The sale price (must be &gt; 0).</param>
/// <param name="SupplyPrice">The cost price (must be &gt; 0).</param>
/// <param name="RetailPrice">The suggested retail price (must be &gt; 0).</param>
/// <param name="QuantityInStock">The initial stock quantity (must be ≥ 0).</param>
/// <param name="LowStockThreshold">The low‑stock threshold (must be ≥ 0).</param>
/// <param name="ExpireDate">An optional expiration date.</param>
public sealed record CreateProductRequest(
    int CategoryId,
    string Name,
    string SKU,
    string Measurement,
    string? Description,
    string? Barcode,
    decimal SalePrice,
    decimal SupplyPrice,
    decimal RetailPrice,
    int QuantityInStock,
    int LowStockThreshold,
    DateOnly? ExpireDate);
