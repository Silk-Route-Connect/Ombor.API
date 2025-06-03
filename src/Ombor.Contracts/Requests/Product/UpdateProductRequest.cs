using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Product;

/// <summary>
/// Request to update an existing product.
/// </summary>
/// <param name="Id">The identifier of the product to update. Must be &gt; 0.</param>
/// <param name="CategoryId">The new category identifier (must be &gt; 0).</param>
/// <param name="Name">The new product name (required).</param>
/// <param name="SKU">The new SKU value (required).</param>
/// <param name="Description">An optional new description.</param>
/// <param name="Barcode">An optional new barcode.</param>
/// <param name="SalePrice">The new sale price (must be &gt; 0).</param>
/// <param name="SupplyPrice">The new supply price (must be &gt; 0).</param>
/// <param name="RetailPrice">The new retail price (must be &gt; 0).</param>
/// <param name="QuantityInStock">The updated stock quantity (must be ≥ 0).</param>
/// <param name="LowStockThreshold">The updated low‑stock threshold (must be ≥ 0).</param>
/// <param name="Measurement">The unit of measurement (e.g. “Piece”, “Kilogram”).</param>
/// <param name="Type">The type of product (e.g. “Sale”, “Supply”, or “SaleAndSupply”).</param>
public sealed record UpdateProductRequest(
    int Id,
    int CategoryId,
    string Name,
    string SKU,
    string? Description,
    string? Barcode,
    decimal SalePrice,
    decimal SupplyPrice,
    decimal RetailPrice,
    int QuantityInStock,
    int LowStockThreshold,
    UnitOfMeasurement Measurement,
    ProductType Type,
    IFormFile[]? Attachments,
    int[]? ImagesToDelete);
