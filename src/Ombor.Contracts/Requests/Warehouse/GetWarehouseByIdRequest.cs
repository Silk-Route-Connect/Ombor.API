namespace Ombor.Contracts.Requests.Warehouse;

/// <summary>
/// Request to retrieve a single warehouse by its identifier.
/// </summary>
/// <param name="Id">The identifier of the warehouse to fetch.</param>
public sealed record GetWarehouseByIdRequest(int Id);