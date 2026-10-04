using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Notification;

namespace Ombor.API.Controllers;

/// <summary>
/// The topbar bell: alerts computed from the current organization's data on every read (nothing stored, nothing to
/// mark read). Read-only.
/// </summary>
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    /// <summary>
    /// Lists the alerts that have something to report — overdue sales, late orders, low stock, today's deliveries —
    /// each with its count, money total where relevant, and up to 10 records.
    /// </summary>
    /// <returns>The alerts, most urgent kinds first; empty when all is well.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationDto[]>> GetAsync()
    {
        var response = await service.GetAsync();

        return Ok(response);
    }
}
