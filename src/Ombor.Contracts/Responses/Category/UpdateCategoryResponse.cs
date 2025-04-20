namespace Ombor.Contracts.Responses.Category;

public sealed record UpdateCategoryResponse(
    int Id,
    string Name,
    string? Description);
