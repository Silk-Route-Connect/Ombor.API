using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Transfer;
using Ombor.Contracts.Responses.Transfer;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints for inter-warehouse stock transfers.
/// </summary>
[ApiController]
[Route("api/transfers")]
public sealed class TransfersController(ITransferService transferService) : ControllerBase
{
    /// <summary>
    /// Retrieves transfers, optionally filtered to a single warehouse.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TransferDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferDto[]>> GetAsync([FromQuery] GetTransfersRequest request)
    {
        var response = await transferService.GetAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a single transfer by its ID.
    /// </summary>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(TransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferDto>> GetByIdAsync([FromRoute] GetTransferByIdRequest request)
    {
        var response = await transferService.GetByIdAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Creates an inter-warehouse transfer, moving stock from one warehouse to another.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransferDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransferDto>> PostAsync([FromBody] CreateTransferRequest request)
    {
        var response = await transferService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = response.Id },
            response);
    }
}
