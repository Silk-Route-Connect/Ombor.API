using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.StockAdjustment;
using Ombor.Contracts.Responses.StockAdjustment;

namespace Ombor.API.Controllers;

/// <summary>
/// Stock adjustments — immutable loss/correction stock events. Create, list and read one (no edit/delete).
/// </summary>
[ApiController]
[Route("api/stock-adjustments")]
public sealed class StockAdjustmentsController(IStockAdjustmentService service) : ControllerBase
{
    /// <summary>Lists stock adjustments (newest-first), optionally filtered by warehouse and/or product.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(StockAdjustmentDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockAdjustmentDto[]>> GetAsync([FromQuery] GetStockAdjustmentsRequest request)
    {
        var response = await service.GetAsync(request);

        return Ok(response);
    }

    /// <summary>Returns one stock adjustment with its balance after (the same figure the list serves).</summary>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(StockAdjustmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockAdjustmentDto>> GetByIdAsync([FromRoute] GetStockAdjustmentByIdRequest request)
    {
        var response = await service.GetByIdAsync(request);

        return Ok(response);
    }

    /// <summary>Records a stock adjustment.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StockAdjustmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockAdjustmentDto>> PostAsync([FromBody] CreateStockAdjustmentRequest request)
    {
        var response = await service.CreateAsync(request);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
