using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;

namespace Ombor.API.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedList<PaymentDto>>> GetAsync(
        [FromQuery] GetPaymentsRequest request)
    {
        var payments = await paymentService.GetAsync(request);

        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(payments.MetaData));

        return Ok(payments);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> CreateAsync(
        [FromBody] CreatePaymentRequest request)
    {
        var payment = await paymentService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetAsync),
            new { id = payment.Id },
            payment);
    }
}
