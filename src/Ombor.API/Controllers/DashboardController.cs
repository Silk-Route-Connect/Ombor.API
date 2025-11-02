using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Contracts.Responses.Reports;

namespace Ombor.API.Controllers;

[Route("api/dashboard")]
[ApiController]
public class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet("daily")]
    public async Task<ActionResult<List<DailySalesDto>>> GetDaily()
    {
        var result = await service.GetDailyReportsAsync();

        return Ok(result);
    }

    [HttpGet("weekly")]
    public async Task<ActionResult<List<WeeklySalesDto>>> GetWeekly()
    {
        var result = await service.GetWeeklyReportsAsync();

        return Ok(result);
    }
}
