using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Activity;
using Ombor.Contracts.Responses.Activity;

namespace Ombor.API.Controllers;

/// <summary>
/// The Activity Log (rules 26–28): who changed what and when — documents, payments, stock events and master data —
/// read from the audit log, one item per operation. Read-only.
/// </summary>
[ApiController]
[Route("api/activity")]
public sealed class ActivityController(IActivityService service) : ControllerBase
{
    /// <summary>
    /// Lists operations newest first, filtered by record (kind, and id for one record's history including its
    /// lines), actor, action and local date range, one page at a time.
    /// </summary>
    /// <param name="request">The filters and page.</param>
    /// <returns>The page of operations and how many match in total.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ActivityPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActivityPageDto>> GetAsync([FromQuery] GetActivityRequest request)
    {
        var response = await service.GetAsync(request);

        return Ok(response);
    }

    /// <summary>Returns one operation with all of its changes (a list item shows at most 50).</summary>
    /// <param name="request">The operation id.</param>
    /// <returns>The operation.</returns>
    [HttpGet("{operationId:guid}")]
    [ProducesResponseType(typeof(ActivityItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityItemDto>> GetOperationAsync([FromRoute] GetActivityOperationRequest request)
    {
        var response = await service.GetOperationAsync(request);

        return Ok(response);
    }
}
