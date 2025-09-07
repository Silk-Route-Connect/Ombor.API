namespace Ombor.Contracts.Requests.Inventory;

/// <summary>
/// Data required to update an existing inventory.
/// </summary>
/// <param name="Id">The identifier of the inventory to update.</param>
/// <param name="Name">The new inventory name.</param>
/// <param name="Location">An optional new location.</param>
/// <param name="IsActive">The inventory status.</param>
public sealed record UpdateInventoryRequest(int Id, string Name, string? Location, bool IsActive);
