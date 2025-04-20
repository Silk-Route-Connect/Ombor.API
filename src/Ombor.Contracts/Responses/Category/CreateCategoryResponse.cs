namespace Ombor.Contracts.Responses.Category;

public sealed record CreateCategoryResponse(
    int Id,
    string Name,
    string? Description);
