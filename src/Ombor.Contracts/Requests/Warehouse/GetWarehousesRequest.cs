namespace Ombor.Contracts.Requests.Warehouse;

/// <summary>
/// Request to retrieve a list of warehouses with optional filtering.
/// </summary>
/// <param name="SearchTerm">
/// Optional case-insensitive term to filter by Name, Location.
/// </param>
public sealed record GetWarehousesRequest(string? SearchTerm = null);