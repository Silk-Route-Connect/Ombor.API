namespace Ombor.Contracts.Responses.Inventory;

/// <summary>
/// DTO representing an inventory item.
/// </summary>
/// <param name="Id">The inventory item ID.</param>
/// <param name="Quantity">The inventory item quantity.</param>
/// <param name="InventoryId">The inventory ID.</param>
/// <param name="ProductId">The product ID.</param>
public sealed record InventoryItemDto(
    int Id,
    int Quantity,
    int InventoryId,
    int ProductId);
