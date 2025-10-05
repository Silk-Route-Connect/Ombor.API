using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Responses.Category;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Defines CRUD operations for category management.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Retrieves all categories, optionally filtered by a search term.
    /// </summary>
    /// <param name="request">
    /// The filtering options. <see cref="GetCategoriesRequest.SearchTerm"/> may be null or whitespace.
    /// </param>
    /// <returns>An array of <see cref="CategoryDto"/> matching the filter.</returns>
    Task<PagedList<CategoryDto>> GetAsync(GetCategoriesRequest request);

    /// <summary>
    /// Retrieves a single category by its identifier.
    /// </summary>
    /// <param name="request">Contains the <see cref="GetCategoryByIdRequest.Id"/> of the category to fetch.</param>
    /// <returns>The matching <see cref="CategoryDto"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Category}">If no category with the given ID exists.</exception>
    Task<CategoryDto> GetByIdAsync(GetCategoryByIdRequest request);

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="request">The properties of the category to create.</param>
    /// <returns>Details of the newly created category.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    Task<CreateCategoryResponse> CreateAsync(CreateCategoryRequest request);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="request">Contains the ID and new values for the category.</param>
    /// <returns>Details of the updated category.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Category}">If no category with the given ID exists.</exception>
    Task<UpdateCategoryResponse> UpdateAsync(UpdateCategoryRequest request);

    /// <summary>
    /// Deletes an existing category by its ID.
    /// </summary>
    /// <param name="request">Contains the <see cref="DeleteCategoryRequest.Id"/> to remove.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Category}">If no category with the given ID exists.</exception>
    Task DeleteAsync(DeleteCategoryRequest request);
}