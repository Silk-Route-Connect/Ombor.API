using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;

namespace Ombor.API.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaymentDto[]>> GetAsync(
        [FromQuery] GetPaymentsRequest request)
    {
        var payments = await paymentService.GetAsync(request);

        return Ok(payments);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<PaymentDto>> GetAsync(int id)
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
