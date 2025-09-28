namespace Ombor.Contracts.Requests.Inventory;

/// <summary>
/// Request to delete an existing inventory.
/// </summary>
/// <param name="Id">The identifier of the inventory to delete.</param>
public sealed record DeleteInventoryRequest(int Id);