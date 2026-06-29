using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints to manage warehouses.
/// </summary>
[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController(
    IWarehouseService warehouseService,
    IMovementService movementService) : ControllerBase
{
    /// <summary>Retrieves warehouses (archived included), with optional search.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(WarehouseDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<WarehouseDto[]>> GetAsync([FromQuery] GetWarehousesRequest request)
    {
        var response = await warehouseService.GetAsync(request);

        return Ok(response);
    }

    /// <summary>Retrieves a specific warehouse by id.</summary>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseDto>> GetWarehouseByIdAsync([FromRoute] GetWarehouseByIdRequest request)
    {
        var response = await warehouseService.GetByIdAsync(request);

        return Ok(response);
    }

    /// <summary>Retrieves the products on hand in a warehouse with their WAC and value.</summary>
    [HttpGet("{id:int:min(1)}/stock")]
    [ProducesResponseType(typeof(WarehouseStockItemDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseStockItemDto[]>> GetStockAsync([FromRoute] int id)
    {
        var response = await warehouseService.GetStockAsync(new GetWarehouseByIdRequest(id));

        return Ok(response);
    }

    /// <summary>Retrieves the warehouse's stock-movement ledger (newest-first) with running per-product balances.</summary>
    [HttpGet("{id:int:min(1)}/movements")]
    [ProducesResponseType(typeof(WarehouseMovementDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseMovementDto[]>> GetMovementsAsync([FromRoute] int id)
    {
        var response = await movementService.GetWarehouseMovementsAsync(id);

        return Ok(response);
    }

    /// <summary>Creates a new (empty) warehouse.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WarehouseDto>> PostAsync([FromBody] CreateWarehouseRequest request)
    {
        var response = await warehouseService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetWarehouseByIdAsync),
            new { id = response.Id },
            response);
    }

    /// <summary>Updates an existing warehouse's name/location.</summary>
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseDto>> PutAsync(
        [FromRoute] int id,
        [FromBody] UpdateWarehouseRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Id mismatch",
                Detail = $"Route Id ({id}) does not match body Id ({request.Id}).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await warehouseService.UpdateAsync(request);

        return Ok(response);
    }

    /// <summary>Records the opening (initial) stock of a warehouse.</summary>
    [HttpPost("{id:int:min(1)}/opening-stock")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseDto>> AddOpeningStockAsync(
        [FromRoute] int id,
        [FromBody] AddOpeningStockRequest request)
    {
        if (id != request.WarehouseId)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Id mismatch",
                Detail = $"Route Id ({id}) does not match body WarehouseId ({request.WarehouseId}).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await warehouseService.AddOpeningStockAsync(request);

        return Ok(response);
    }

    /// <summary>Archives a warehouse (soft-delete).</summary>
    [HttpPost("{id:int:min(1)}/archive")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseDto>> ArchiveAsync([FromRoute] int id)
    {
        var response = await warehouseService.ArchiveAsync(id);

        return Ok(response);
    }

    /// <summary>Restores an archived warehouse.</summary>
    [HttpPost("{id:int:min(1)}/restore")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseDto>> RestoreAsync([FromRoute] int id)
    {
        var response = await warehouseService.RestoreAsync(id);

        return Ok(response);
    }

    /// <summary>Hard-deletes an unreferenced warehouse; a referenced warehouse is rejected with 409 (archive instead).</summary>
    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteAsync([FromRoute] DeleteWarehouseRequest request)
    {
        await warehouseService.DeleteAsync(request);

        return NoContent();
    }
}
