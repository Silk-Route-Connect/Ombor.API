using Ombor.Contracts.Common;

namespace Ombor.Contracts.Responses.Product;

/// <summary>
/// DTO representing a product for client consumption.
/// </summary>
/// <param name="Id">The product ID.</param>
/// <param name="CategoryId">The category ID.</param>
/// <param name="CategoryName">The category’s name.</param>
/// <param name="Name">The product name.</param>
/// <param name="SKU">The SKU.</param>
/// <param name="Description">The description, if any.</param>
/// <param name="Barcode">The barcode, if any.</param>
/// <param name="SalePrice">The sale price.</param>
/// <param name="SupplyPrice">The supply price.</param>
/// <param name="RetailPrice">The retail price.</param>
/// <param name="LowStockThreshold">The low‑stock threshold.</param>
/// <param name="IsLowStock">Whether total stock ≤ threshold.</param>
/// <param name="Measurement">The unit of measurement (e.g. “Unit”, “Kilogram”).</param>
/// <param name="Type">The type of product (e.g. “Sale”, “Supply”, or “All”).</param>
/// <param name="IsArchived">Whether the product is archived.</param>
/// <param name="Images">Associated product images.</param>
/// <param name="InventoryItems">Per-warehouse stock for the product.</param>
/// <param name="TotalStock">Total stock summed across warehouses (rule 17).</param>
/// <param name="AverageCost">Value-weighted average cost across warehouses; null when there is no stock.</param>
/// <param name="Packaging">Optional packaging info; <see langword="null"/> when not applicable.</param>
public sealed record ProductDto(
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
    int LowStockThreshold,
    bool IsLowStock,
    string Measurement,
    string Type,
    bool IsArchived,
    ProductImageDto[] Images,
    ProductInventoryItemDto[] InventoryItems,
    int TotalStock,
    decimal? AverageCost,
    ProductPackagingDto? Packaging);
