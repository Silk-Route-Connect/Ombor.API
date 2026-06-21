using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;

namespace Ombor.API.Controllers;

[Route("api/orders")]
[ApiController]
public class OrderStatesController(IOrderService service) : ControllerBase
{
    [HttpPost("{id}/process")]
    public async Task<ActionResult<OrderDto>> ProcessAsync([FromRoute] int id)
    {
        var request = new ProcessOrderRequest(id);
        var response = await service.ProcessAsync(request);

        return Ok(response);
    }

    [HttpPost("{id}/ship")]
    public async Task<ActionResult<OrderDto>> ShipAsync([FromRoute] int id)
    {
        var request = new ShipOrderRequest(id);
        var response = await service.ShipAsync(request);

        return Ok(response);
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<OrderDto>> RejectAsync([FromRoute] int id)
    {
        var request = new RejectOrderRequest(id);
        var response = await service.RejectAsync(request);

        return Ok(response);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<OrderDto>> CancelAsync([FromRoute] int id)
    {
        var request = new CancelOrderRequest(id);
        var response = await service.CancelAsync(request);

        return Ok(response);
    }

    [HttpPost("{id}/deliver")]
    public async Task<ActionResult<OrderDto>> DeliverAsync([FromRoute] int id, [FromBody] DeliverOrderBody body)
    {
        var request = new DeliverOrderRequest(id, body.WarehouseId);
        var response = await service.DeliverAsync(request);

        return Ok(response);
    }

    [HttpPost("{id}/return")]
    public async Task<ActionResult<OrderDto>> ReturnAsync([FromRoute] int id)
    {
        var request = new ReturnOrderRequest(id);
        var response = await service.ReturnAsync(request);

        return Ok(response);
    }
}
