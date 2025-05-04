using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class CategoryMappings
{
    public static CategoryDto ToDto(this Category category) =>
        new(category.Id,
            category.Name,
            category.Description);

    public static Category ToEntity(this CreateCategoryRequest request) =>
        new()
        {
            Name = request.Name,
            Description = request.Description
        };

    public static CreateCategoryResponse ToCreateResponse(this Category category) =>
        new(category.Id,
            category.Name,
            category.Description);

    public static UpdateCategoryResponse ToUpdateResponse(this Category category) =>
        new(category.Id,
            category.Name,
            category.Description);

    public static void ApplyUpdate(this Category category, UpdateCategoryRequest request)
    {
        category.Name = request.Name;
        category.Description = request.Description;
    }
}
