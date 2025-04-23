namespace Ombor.Contracts.Responses.Product;

/// <summary>
/// DTO representing a product for client consumption.
/// </summary>
/// <param name="Id">The product ID.</param>
/// <param name="CategoryId">The category ID.</param>
/// <param name="CategoryName">The category’s name.</param>
/// <param name="Name">The product name.</param>
/// <param name="SKU">The SKU.</param>
/// <param name="Measurement">The measurement unit.</param>
/// <param name="Description">The description, if any.</param>
/// <param name="Barcode">The barcode, if any.</param>
/// <param name="SalePrice">The sale price.</param>
/// <param name="SupplyPrice">The supply price.</param>
/// <param name="RetailPrice">The retail price.</param>
/// <param name="QuantityInStock">The stock level.</param>
/// <param name="LowStockThreshold">The low‑stock threshold.</param>
/// <param name="ExpireDate">The expiration date, if any.</param>
/// <param name="IsLowStock">Whether stock ≤ threshold.</param>
/// <param name="IsExpirationClose">Whether expiration ≥ 7 days ago.</param>
public sealed record ProductDto(
    int Id,
    int CategoryId,
    string CategoryName,
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
    DateOnly? ExpireDate,
    bool IsLowStock,
    bool IsExpirationClose);
