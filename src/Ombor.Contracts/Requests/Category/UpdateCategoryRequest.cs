namespace Ombor.Contracts.Requests.Category;

/// <summary>
/// Data required to update an existing category.
/// </summary>
/// <param name="Id">The identifier of the category to update. Must be &gt; 0.</param>
/// <param name="Name">The new category name (required).</param>
/// <param name="Description">An optional new description.</param>
public sealed record UpdateCategoryRequest(int Id, string Name, string? Description);
