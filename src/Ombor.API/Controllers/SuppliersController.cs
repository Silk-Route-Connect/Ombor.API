using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Supplier;
using Ombor.Contracts.Responses.Supplier;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints to manage suppliers.
/// </summary>
[ApiController]
[Route("api/suppliers")]
public sealed class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of suppliers, with optional filtering by search term.
    /// </summary>
    /// <param name="request">Filtering and paging parameters.</param>
    /// <returns>Array of <see cref="SupplierDto"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SupplierDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDto[]>> GetAsync(
        [FromQuery] GetSuppliersRequest request)
    {
        var response = await supplierService.GetAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific supplier by its unique ID.
    /// </summary>
    /// <param name="request">Request containing the supplier ID.</param>
    /// <returns>The matching <see cref="SupplierDto"/>.</returns>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> GetSupplierByIdAsync([FromRoute] GetSupplierByIdRequest request)
    {
        var response = await supplierService.GetByIdAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    /// <param name="request">Payload describing the supplier to create.</param>
    /// <returns>The created supplier details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSupplierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateSupplierResponse>> PostAsync(
        [FromBody] CreateSupplierRequest request)
    {
        var response = await supplierService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetSupplierByIdAsync),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Updates an existing supplier.
    /// </summary>
    /// <param name="id">The ID of the supplier to update.</param>
    /// <param name="request">Payload describing the supplier to update(must include the ID of the supplier).</param>
    /// <returns>The updated supplier details.</returns>
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(typeof(UpdateSupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateSupplierResponse>> PutAsync(
        [FromRoute] int id,
        [FromBody] UpdateSupplierRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Id mismatch",
                Detail = $"Route ID ({id}) does not match body ID ({request.Id}).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await supplierService.UpdateAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Deletes a supplier by its ID.
    /// </summary>
    /// <param name="request">Delete request containing the supplier ID.</param>
    /// <returns></returns>
    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(
            [FromRoute] DeleteSupplierRequest request)
    {
        await supplierService.DeleteAsync(request);

        return NoContent();
    }
}