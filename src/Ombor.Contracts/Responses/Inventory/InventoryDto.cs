namespace Ombor.Contracts.Responses.Inventory;

/// <summary>
/// DTO representing an inventory.
/// </summary>
/// <param name="Id">The inventory ID.</param>
/// <param name="Name">The inventory name.</param>
/// <param name="Location">The inventory location if any.</param>
/// <param name="IsActive">The inventory status.</param>
public sealed record InventoryDto(
    int Id,
    string Name,
    string? Location,
    bool IsActive,
    InventoryItemDto[] InventoryItems);
