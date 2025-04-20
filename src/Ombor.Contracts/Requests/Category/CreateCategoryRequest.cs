namespace Ombor.Contracts.Requests.Category;

public sealed record CreateCategoryRequest(
    string Name,
    string? Description);
