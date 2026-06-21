namespace Ombor.Contracts.Responses.Warehouse;

/// <summary>One product's stock on hand in a warehouse (warehouse-local WAC + value).</summary>
/// <param name="ProductId">The product id.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Sku">The product SKU.</param>
/// <param name="CategoryName">The product's category name, if any.</param>
/// <param name="Measurement">The product's unit of measurement.</param>
/// <param name="Quantity">Units on hand in this warehouse.</param>
/// <param name="AverageCost">Warehouse-local weighted-average cost.</param>
/// <param name="Value">Stock value (quantity × average cost).</param>
public sealed record WarehouseStockItemDto(
    int ProductId,
    string ProductName,
    string Sku,
    string? CategoryName,
    string Measurement,
    int Quantity,
    decimal AverageCost,
    decimal Value);
