namespace Ombor.Contracts.Requests.Warehouse;

/// <summary>
/// Request to delete a warehouse by its identifier.
/// </summary>
/// <param name="Id">The identifier of the warehouse to delete.</param>
public sealed record DeleteWarehouseRequest(int Id);
