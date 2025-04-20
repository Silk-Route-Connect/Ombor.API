namespace Ombor.Contracts.Requests.Category;

/// <summary>
/// Request to retrieve a (paged, filtered, sorted) list of categories.
/// </summary>
/// <param name="SearchTerm">
///   Optional case‑insensitive term to filter categories by name or description.
/// </param>
public sealed record GetCategoriesRequest(string? SearchTerm);
