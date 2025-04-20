using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class CategoryMappings
{
    public static Category ToEntity(this CreateCategoryRequest request)
    {
        return new Category
        {
            Name = request.Name,
            Description = request.Description
        };
    }

    public static Category ToEntity(this UpdateCategoryRequest request)
    {
        return new Category
        {
            Id = request.Id,
            Name = request.Name,
            Description = request.Description
        };
    }

    public static CreateCategoryResponse ToCreateResponse(this Category category)
    {
        return new CreateCategoryResponse(
            category.Id,
            category.Name,
            category.Description);
    }

    public static UpdateCategoryResponse ToUpdateResponse(this Category category)
    {
        return new UpdateCategoryResponse(
            category.Id,
            category.Name,
            category.Description);
    }

    public static CategoryDto ToDto(this Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description);
    }

    public static void Update(this Category category, UpdateCategoryRequest request)
    {
        category.Name = request.Name;
        category.Description = request.Description;
    }
}
