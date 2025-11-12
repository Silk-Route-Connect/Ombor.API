using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Order;

namespace Ombor.API.Controllers;

[Route("api/orders")]
[ApiController]
public class OrderStatesController(IOrderService service) : ControllerBase
{
    [HttpPost("{id}/process")]
    public async Task<ActionResult> ProcessAsync([FromRoute] int id)
    {
        var request = new ProcessOrderRequest(id);
        await service.ProcessAsync(request);

        return NoContent();
    }

    [HttpPost("{id}/ship")]
    public async Task<ActionResult> ShipAsync([FromRoute] int id)
    {
        var request = new ShipOrderRequest(id);
        await service.ShipAsync(request);

        return NoContent();
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectAsync([FromRoute] int id)
    {
        var request = new RejectOrderRequest(id);
        await service.RejectAsync(request);

        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelAsync([FromRoute] int id)
    {
        var request = new CancelOrderRequest(id);
        await service.CancelAsync(request);

        return NoContent();
    }

    [HttpPost("{id}/deliver")]
    public async Task<ActionResult> DeliverAsync([FromRoute] int id)
    {
        var request = new DeliverOrderRequest(id);
        await service.DeliverAsync(request);

        return NoContent();
    }

    [HttpPost("{id}/return")]
    public async Task<ActionResult> ReturnAsync([FromRoute] int id)
    {
        var request = new ReturnOrderRequest(id);
        await service.ReturnAsync(request);

        return NoContent();
    }
}
