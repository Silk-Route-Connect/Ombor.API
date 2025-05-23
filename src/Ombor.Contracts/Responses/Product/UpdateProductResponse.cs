using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Product;

/// <summary>
/// Response returned after successfully updating a product.
/// </summary>
/// <param name="Id">The product ID.</param>
/// <param name="CategoryId">The category ID.</param>
/// <param name="CategoryName">The category’s name.</param>
/// <param name="Name">The updated name.</param>
/// <param name="SKU">The updated SKU.</param>
/// <param name="Measurement">The updated measurement.</param>
/// <param name="Description">The updated description, if any.</param>
/// <param name="Barcode">The updated barcode, if any.</param>
/// <param name="SalePrice">The updated sale price.</param>
/// <param name="SupplyPrice">The updated supply price.</param>
/// <param name="RetailPrice">The updated retail price.</param>
/// <param name="QuantityInStock">The updated stock level.</param>
/// <param name="LowStockThreshold">The updated low‑stock threshold.</param>
/// <param name="IsLowStock">Whether stock ≤ threshold post‑update.</param>
public sealed record UpdateProductResponse(
    int Id,
    int CategoryId,
    string CategoryName,
    string Name,
    string SKU,
    string? Description,
    string? Barcode,
    decimal SalePrice,
    decimal SupplyPrice,
    decimal RetailPrice,
    int QuantityInStock,
    int LowStockThreshold,
    bool IsLowStock,
    UnitOfMeasurement Measurement,
    ProductType Type);
