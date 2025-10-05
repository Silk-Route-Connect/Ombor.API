namespace Ombor.Contracts.Responses.Inventory;

/// <summary>
/// Response returned after successfully creating an inventory.
/// </summary>
/// <param name="Id">The newly created inventory's ID.</param>
/// <param name="Name">The inventory's name.</param>
/// <param name="Location">The inventory's location, if any.</param>
/// <param name="IsActive">The inventory's status.</param>
/// <param name="InventoryItems">The inventory's items.</param>
public sealed record CreateInventoryResponse(
    int Id,
    string Name,
    string? Location,
    bool IsActive,
    InventoryItemDto[] InventoryItems);
