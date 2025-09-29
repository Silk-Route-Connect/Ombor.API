namespace Ombor.Contracts.Requests.Inventory;

/// <summary>
/// Request to retrieve a single inventory by its identifier.
/// </summary>
/// <param name="Id">The identifier of the inventory to fetch.</param>
public sealed record GetInventoryByIdRequest(int Id);