using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Debt;

namespace Ombor.API.Controllers;

/// <summary>
/// Debts — derived read models over the ledger (no stored entity): the unpaid-document list and the net-position summary.
/// </summary>
[ApiController]
[Route("api/debts")]
public sealed class DebtsController(IDebtService service) : ControllerBase
{
    /// <summary>Lists all outstanding debts of the current organization (newest-first).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DebtDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<DebtDto[]>> GetAsync()
    {
        var response = await service.GetDebtsAsync();

        return Ok(response);
    }

    /// <summary>
    /// Who owes whom: net partner positions with receivable/payable totals and aging — the figures the Partners,
    /// Debts and Dashboard pages share — plus the unpaid-document totals.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DebtSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DebtSummaryDto>> GetSummaryAsync()
    {
        var response = await service.GetSummaryAsync();

        return Ok(response);
    }
}
