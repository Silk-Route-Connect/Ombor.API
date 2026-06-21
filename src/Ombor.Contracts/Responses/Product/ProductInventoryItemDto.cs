namespace Ombor.Contracts.Responses.Product;

/// <summary>A product's stock in one warehouse, with that warehouse's weighted-average cost.</summary>
/// <param name="InventoryId">The warehouse id.</param>
/// <param name="InventoryName">The warehouse name.</param>
/// <param name="Quantity">The quantity held in this warehouse.</param>
/// <param name="AverageCost">The weighted-average unit cost in this warehouse.</param>
public sealed record ProductInventoryItemDto(
    int InventoryId,
    string InventoryName,
    int Quantity,
    decimal AverageCost);
