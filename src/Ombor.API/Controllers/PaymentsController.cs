using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Payments;
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
        var response = await paymentService.GetAsync(request);

        return Ok(response);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentByIdAsync(
        [FromRoute] int id)
    {
        var request = new GetPaymentByIdRequest(id);
        var response = await paymentService.GetByIdAsync(request);

        return Ok(response);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CreatePaymentResponse>> PostAsync(
        [FromForm] CreatePaymentRequest request)
    {
        var response = await paymentService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetPaymentByIdAsync),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:int:min(1)}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UpdatePaymentResponse>> PutAsync(
        [FromRoute] int id,
        [FromForm] UpdatePaymentRequest request)
    {
        if (request.Id != id)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "ID mismatch",
                Detail = $"Route ID ({id}) does not match body ID ({request.Id}).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await paymentService.UpdateAsync(request);

        return Ok(response);
    }

    [HttpDelete("{id:int:min(1)}")]
    public async Task<ActionResult> DeleteAsync(
        [FromRoute] int id)
    {
        var request = new DeletePaymentRequest(id);
        await paymentService.DeleteAsync(request);

        return NoContent();
    }
}
