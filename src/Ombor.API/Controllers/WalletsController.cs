using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Wallet;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints to manage wallets (money locations) and inter-wallet transfers.
/// </summary>
[ApiController]
[Authorize]
[Route("api/wallets")]
public sealed class WalletsController(IWalletService walletService) : ControllerBase
{
    /// <summary>Returns all wallets (including archived), optionally filtered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(WalletDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletDto[]>> GetAsync([FromQuery] GetWalletsRequest request)
    {
        var response = await walletService.GetAsync(request);

        return Ok(response);
    }

    /// <summary>Retrieves a single wallet by its identifier.</summary>
    [HttpGet("{Id:int:min(1)}")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletDto>> GetWalletByIdAsync([FromRoute] GetWalletByIdRequest request)
    {
        var response = await walletService.GetByIdAsync(request);

        return Ok(response);
    }

    /// <summary>Returns a wallet's running operation ledger (newest-first).</summary>
    [HttpGet("{id:int:min(1)}/operations")]
    [ProducesResponseType(typeof(WalletOperationDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletOperationDto[]>> GetWalletOperationsAsync([FromRoute] int id)
    {
        var response = await walletService.GetOperationsAsync(id);

        return Ok(response);
    }

    /// <summary>Returns the transfers touching a wallet (newest-first).</summary>
    [HttpGet("{id:int:min(1)}/transfers")]
    [ProducesResponseType(typeof(WalletTransferDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletTransferDto[]>> GetWalletTransfersAsync([FromRoute] int id)
    {
        var response = await walletService.GetTransfersAsync(id);

        return Ok(response);
    }

    /// <summary>Creates a new wallet.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletDto>> PostAsync([FromBody] CreateWalletRequest request)
    {
        var response = await walletService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetWalletByIdAsync),
            new { id = response.Id },
            response);
    }

    /// <summary>Moves money between two wallets (atomic, immutable; hard-blocked over the source balance).</summary>
    [HttpPost("transfers")]
    [ProducesResponseType(typeof(WalletTransferDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletTransferDto>> PostTransferAsync([FromBody] CreateWalletTransferRequest request)
    {
        var response = await walletService.CreateTransferAsync(request);

        return CreatedAtAction(
            nameof(GetWalletTransfersAsync),
            new { id = response.FromWalletId },
            response);
    }

    /// <summary>Updates a wallet's name. Type and opening balance are immutable.</summary>
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletDto>> PutAsync(
        [FromRoute] int id,
        [FromBody] UpdateWalletRequest request)
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

        var response = await walletService.UpdateAsync(request);

        return Ok(response);
    }

    /// <summary>Archives a wallet. Archived wallets are hidden from default lists but still count in totals.</summary>
    [HttpPost("{id:int:min(1)}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveAsync([FromRoute] int id)
    {
        await walletService.ArchiveAsync(id);

        return NoContent();
    }

    /// <summary>Restores a previously archived wallet.</summary>
    [HttpPost("{id:int:min(1)}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreAsync([FromRoute] int id)
    {
        await walletService.RestoreAsync(id);

        return NoContent();
    }

    /// <summary>Hard-deletes an unreferenced wallet; a referenced wallet is rejected with 409 (archive instead).</summary>
    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync([FromRoute] int id)
    {
        await walletService.DeleteAsync(id);

        return NoContent();
    }
}
