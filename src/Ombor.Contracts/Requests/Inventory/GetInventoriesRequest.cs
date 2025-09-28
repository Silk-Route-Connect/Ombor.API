namespace Ombor.Contracts.Requests.Inventory;

/// <summary>
/// Request to retrieve a list of inventories with optional filtering.
/// </summary>
/// <param name="SearchTerm">
/// Optional case-insensitive term to filter by Name, Location.
/// </param>
public sealed record GetInventoriesRequest(string? SearchTerm = null);