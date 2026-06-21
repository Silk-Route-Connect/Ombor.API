using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Dashboard;

namespace Ombor.API.Controllers;

/// <summary>
/// Dashboard — an aggregated read model (no stored entity). Debt figures reconcile with <c>/api/debts</c>.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    /// <summary>
    /// Returns the dashboard snapshot. <paramref name="period"/> (today/week/month, default month)
    /// drives only the revenue KPI and the time-series; debt figures are a current snapshot.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> GetAsync([FromQuery] DashboardPeriod period = DashboardPeriod.Month)
    {
        var response = await service.GetAsync(period);

        return Ok(response);
    }
}
