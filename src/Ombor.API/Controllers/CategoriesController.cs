using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints to maanage product categories.
/// </summary>
[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>
    /// Returns a list of categories, optionally filtered by a search term.
    /// </summary>
    /// <param name="request">Paging, filtering and sorting parameters.</param>
    /// <returns>Paged list of <see cref="CategoryDto"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CategoryDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoryDto[]>> GetAsync(
        [FromQuery] GetCategoriesRequest request)
    {
        var response = await categoryService.GetAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific category by its unique identifier.
    /// </summary>
    /// <param name="request">Request containing the category ID.</param>
    /// <returns>The matching <see cref="CategoryDto"/>.</returns>
    [HttpGet("{Id:int:min(1)}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategoryByIdAsync(
        [FromRoute] GetCategoryByIdRequest request)
    {
        var response = await categoryService.GetByIdAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="request">The payload for the new category.</param>
    /// <returns>The created category details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCategoryResponse>> PostAsync(
        [FromBody] CreateCategoryRequest request)
    {
        var response = await categoryService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetCategoryByIdAsync),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The ID of the category to update.</param>
    /// <param name="request">The updated category data.</param>
    /// <returns>The updated category details.</returns>
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(typeof(UpdateCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateCategoryResponse>> PutAsync(
        [FromRoute] int id,
        [FromBody] UpdateCategoryRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "ID mismatch",
                Detail = $"Route ID ({id}) does not match body ID ({request.Id}).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await categoryService.UpdateAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Deletes a category by its ID.
    /// </summary>
    /// <param name="request">The delete request containing the category ID.</param>
    [HttpDelete("{Id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] DeleteCategoryRequest request)
    {
        await categoryService.DeleteAsync(request);

        return NoContent();
    }
}
