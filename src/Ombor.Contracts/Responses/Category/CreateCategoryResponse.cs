namespace Ombor.Contracts.Responses.Category;

/// <summary>
/// Response returned after successfully creating a category.
/// </summary>
/// <param name="Id">The newly created category’s ID.</param>
/// <param name="Name">The category name.</param>
/// <param name="Description">The category description, if any.</param>
public sealed record CreateCategoryResponse(int Id, string Name, string? Description);
