using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;

namespace Ombor.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryDto[]> GetAsync(GetCategoriesRequest request);
    Task<CategoryDto> GetByIdAsync(GetCategoryByIdRequest request);
    Task<CreateCategoryResponse> CreateAsync(CreateCategoryRequest request);
    Task<UpdateCategoryResponse> UpdateAsync(UpdateCategoryRequest request);
    Task DeleteAsync(DeleteCategoryRequest request);
}
