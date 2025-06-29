namespace Ombor.Contracts.Requests.Inventory;

/// <summary>
/// Data required to delete an inventory.
/// </summary>
/// <param name="Id">The identifier of the inventory to delete.</param>
public sealed record DeleteInventoryRequest(int Id);