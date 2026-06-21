namespace Ombor.Contracts.Requests.Warehouse;

/// <summary>
/// Request to update an existing warehouse. Archive state is managed via archive/restore, not here.
/// </summary>
/// <param name="Id">The identifier of the warehouse to update.</param>
/// <param name="Name">The new warehouse name.</param>
/// <param name="Location">An optional new location.</param>
public sealed record UpdateWarehouseRequest(int Id, string Name, string? Location);
