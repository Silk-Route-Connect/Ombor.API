namespace Ombor.Contracts.Requests.Inventory;

/// <summary>
/// Data required to create a new inventory.
/// </summary>
/// <param name="Name">The inventory name (required).</param>
/// <param name="Location">An optional location.</param>
/// <param name="IsActive">The inventory status.</param>
public sealed record CreateInventoryRequest(string Name, string? Location, bool IsActive);