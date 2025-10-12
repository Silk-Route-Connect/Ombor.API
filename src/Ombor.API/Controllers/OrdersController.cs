using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;

namespace Ombor.API.Controllers;

[Route("api/orders")]
[ApiController]
public class OrdersController(IOrderService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OrderDto[]>> GetAsync([FromQuery] GetOrdersRequest request)
    {
        var response = await service.GetAsync(request);

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrderByIdAsync([FromRoute] int id)
    {
        var request = new GetOrderByIdRequest(id);
        var response = await service.GetByIdAsync(request);

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrderAsync([FromBody] CreateOrderRequest request)
    {
        var response = await service.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetOrderByIdAsync),
            new { id = response.Id },
            response);
    }
}
