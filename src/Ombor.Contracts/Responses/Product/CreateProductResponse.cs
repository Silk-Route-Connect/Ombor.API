namespace Ombor.Contracts.Responses.Product;

public sealed record CreateProductResponse(
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
