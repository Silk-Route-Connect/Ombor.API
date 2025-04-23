namespace Ombor.Contracts.Requests.Category;

/// <summary>
/// Data required to create a new category.
/// </summary>
/// <param name="Name">The category name (required).</param>
/// <param name="Description">An optional description.</param>
public sealed record CreateCategoryRequest(string Name, string? Description);
