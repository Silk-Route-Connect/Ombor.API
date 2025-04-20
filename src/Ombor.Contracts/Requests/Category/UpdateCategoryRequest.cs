namespace Ombor.Contracts.Requests.Category;

public sealed record UpdateCategoryRequest(
    int Id,
    string Name,
    string? Description);
