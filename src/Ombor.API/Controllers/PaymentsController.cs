using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints to record and read payments (source/allocation model, rules 8-14).
/// </summary>
[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    /// <summary>Returns payments newest-first, optionally filtered.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaymentRecordDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentRecordDto[]>> GetAsync([FromQuery] GetPaymentsRequest request)
    {
        var response = await paymentService.GetRecordsAsync(request);

        return Ok(response);
    }

    /// <summary>Returns the reference data the payment-create form needs.</summary>
    [HttpGet("form-data")]
    [ProducesResponseType(typeof(PaymentFormDataDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentFormDataDto>> GetFormDataAsync()
    {
        var response = await paymentService.GetFormDataAsync();

        return Ok(response);
    }

    /// <summary>Returns a partner's open transactions (oldest-first) a payment can settle.</summary>
    [HttpGet("outstanding")]
    [ProducesResponseType(typeof(OutstandingTransactionDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OutstandingTransactionDto[]>> GetOutstandingAsync([FromQuery] int? partnerId)
    {
        if (partnerId is null or <= 0)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["partnerId"] = ["partnerId is required."],
            }));
        }

        var response = await paymentService.GetOutstandingAsync(partnerId.Value);

        return Ok(response);
    }

    /// <summary>Retrieves a single payment by its identifier.</summary>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(PaymentRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentRecordDto>> GetPaymentByIdAsync([FromRoute] int id)
    {
        var response = await paymentService.GetRecordByIdAsync(id);

        return Ok(response);
    }

    /// <summary>Records a standalone payment, settling transactions and parking any excess as advance.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentRecordDto>> PostAsync([FromBody] CreatePaymentRecordRequest request)
    {
        var response = await paymentService.CreateRecordAsync(request);

        return CreatedAtAction(
            nameof(GetPaymentByIdAsync),
            new { id = response.Id },
            response);
    }
}
