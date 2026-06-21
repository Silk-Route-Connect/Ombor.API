namespace Ombor.Contracts.Requests.Warehouse;

/// <summary>
/// Request to create a new warehouse. Created empty (no stock).
/// </summary>
/// <param name="Name">The warehouse name (required, unique).</param>
/// <param name="Location">An optional location.</param>
public sealed record CreateWarehouseRequest(string Name, string? Location);
