namespace Ombor.Contracts.Requests.Product;

public sealed record UpdateProductRequest(
    int Id,
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