namespace Ombor.Contracts.Responses.Inventory;

/// <summary>
/// Response returned after successfully updating an inventory.
/// </summary>
/// <param name="Id">The inventory's ID.</param>
/// <param name="Name">The updated name.</param>
/// <param name="Location">The updated location, if any.</param>
/// <param name="IsActive">The updated status.</param>
public sealed record UpdateInventoryResponse(
    int Id,
    string Name,
    string? Location,
    bool IsActive,
    InventoryItemDto[] InventoryItems);
