namespace Ombor.Contracts.Responses.Category;

/// <summary>
/// Response returned after successfully updating a category.
/// </summary>
/// <param name="Id">The category’s ID.</param>
/// <param name="Name">The updated name.</param>
/// <param name="Description">The updated description, if any.</param>
public sealed record UpdateCategoryResponse(int Id, string Name, string? Description);
