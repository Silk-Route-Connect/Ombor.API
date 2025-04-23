namespace Ombor.Contracts.Requests.Category;

/// <summary>
/// Request to retrieve a filtered list of categories.
/// </summary>
/// <param name="SearchTerm">
/// Optional case‐insensitive term to filter by name or description.
/// </param>
public sealed record GetCategoriesRequest(string? SearchTerm);
