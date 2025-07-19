namespace Ombor.Contracts.Requests.Inventory;

/// <summary>
/// Request to retrieve a single inventory by its identifier.
/// </summary>
/// <param name="Id">The identifier of the intventory to fetch.</param>
public sealed record GetInventoryByIdRequest(int Id);