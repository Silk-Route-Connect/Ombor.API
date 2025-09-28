using Ombor.Contracts.Common;
using Ombor.Contracts.Responses.Inventory;

namespace Ombor.Contracts.Responses.Product;

/// <summary>
/// Response returned after successfully creating a product.
/// </summary>
/// <param name="Id">The newly created product’s ID.</param>
/// <param name="CategoryId">The category ID.</param>
/// <param name="CategoryName">The category’s name.</param>
/// <param name="Name">The product’s name.</param>
/// <param name="SKU">The product’s SKU.</param>
/// <param name="Description">The product’s description, if any.</param>
/// <param name="Barcode">The product’s barcode, if any.</param>
/// <param name="SalePrice">The sale price.</param>
/// <param name="SupplyPrice">The supply price.</param>
/// <param name="RetailPrice">The retail price.</param>
/// <param name="QuantityInStock">The stock level.</param>
/// <param name="LowStockThreshold">The low‑stock threshold.</param>
/// <param name="IsLowStock">Whether stock ≤ threshold.</param>
/// <param name="Measurement">The unit of measurement (e.g. “Piece”, “Kilogram”).</param>
/// <param name="Type">The type of product (e.g. “Sale”, “Supply”, or “SaleAndSupply”).</param>
public sealed record CreateProductResponse(
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
    string Measurement,
    string Type,
    ProductImageDto[] Images,
    InventoryItemDto[] InventoryItems,
    ProductPackagingDto? Packaging);
