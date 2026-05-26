namespace Ombor.Contracts.Responses.Inventory;

/// <summary>
/// DTO representing an inventory item.
/// </summary>
/// <param name="Id">The inventory item ID.</param>
/// <param name="Quantity">The inventory item quantity.</param>
/// <param name="AverageCost">The weighted-average unit cost of the stock on hand.</param>
/// <param name="InventoryId">The inventory ID.</param>
/// <param name="ProductId">The product ID.</param>
public sealed record InventoryItemDto(
    int Id,
    int Quantity,
    decimal AverageCost,
    int InventoryId,
    int ProductId);
