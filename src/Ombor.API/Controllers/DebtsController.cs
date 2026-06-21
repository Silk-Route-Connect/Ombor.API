using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Debt;

namespace Ombor.API.Controllers;

/// <summary>
/// Debts — a derived read model over unpaid/partially-paid transactions (no stored entity).
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
}
