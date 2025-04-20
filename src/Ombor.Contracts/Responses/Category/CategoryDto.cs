namespace Ombor.Contracts.Responses.Category;

public sealed record CategoryDto(
    int Id,
    string Name,
    string? Description);