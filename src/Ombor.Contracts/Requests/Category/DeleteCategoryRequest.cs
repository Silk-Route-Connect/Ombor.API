namespace Ombor.Contracts.Requests.Category;

/// <summary>
/// Data required to delete a category.
/// </summary>
/// <param name="Id">The identifier of the category to delete. Must be &gt; 0.</param>
public sealed record DeleteCategoryRequest(int Id);
