using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints to manage inventories.
/// </summary>
[ApiController]
[Route("api/inventories")]
public sealed class InventoriesController(IInventoryService inventoryService) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of inventories, with optional filtering by search term.
    /// </summary>
    /// <param name="request">Filtering and paging parameters.</param>
    /// <returns>Array of <see cref="InventoryDto"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(InventoryDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryDto[]>> GetAsync(
        [FromQuery] GetInventoriesRequest request)
    {
        var response = await inventoryService.GetAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific inventory by its unique ID.
    /// </summary>
    /// <param name="request">Request containing the inventory ID.</param>
    /// <returns>The matching <see cref="InventoryDto"/>.</returns>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> GetInventoryByIdAsync(
        [FromRoute] GetInventoryByIdRequest request)
    {
        var response = await inventoryService.GetByIdAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Creates a new inventory.
    /// </summary>
    /// <param name="request">Payload describing the inventory to create.</param>
    /// <returns>The created inventory details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateInventoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateInventoryResponse>> PostAsync(
        [FromBody] CreateInventoryRequest request)
    {
        var response = await inventoryService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetInventoryByIdAsync),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Updates an existing inventory.
    /// </summary>
    /// <param name="id">The ID of the inventory to update.</param>
    /// <param name="request">Payload describing the inventory to update.</param>
    /// <returns>The updated inventory details.</returns>
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(typeof(UpdateInventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateInventoryResponse>> PutAsync(
        [FromRoute] int id,
        [FromBody] UpdateInventoryRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Id mismatch",
                Detail = $"Route Id ({id}) does not match body Id ({request.Id}).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await inventoryService.UpdateAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Deletes an inventory by ID.
    /// </summary>
    /// <param name="request">Delete request containing the inventory ID.</param>
    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(
            [FromRoute] DeleteInventoryRequest request)
    {
        await inventoryService.DeleteAsync(request);

        return NoContent();
    }
}
