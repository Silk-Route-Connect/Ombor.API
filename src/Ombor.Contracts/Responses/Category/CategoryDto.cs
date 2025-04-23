namespace Ombor.Contracts.Responses.Category;

/// <summary>
/// DTO representing a category for client consumption.
/// </summary>
/// <param name="Id">The category identifier.</param>
/// <param name="Name">The category name.</param>
/// <param name="Description">The category description, if any.</param>
public sealed record CategoryDto(int Id, string Name, string? Description);
