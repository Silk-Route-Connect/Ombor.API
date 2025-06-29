using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Payments;
using Ombor.Contracts.Requests.Transactions;
using Ombor.Contracts.Responses.Payment;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.API.Controllers;

[Route("api/transactions")]
[ApiController]
public class TransactionsController(
    ITransactionService transactionService,
    IPaymentService paymentService,
    ITransactionPaymentService transactionPaymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TransactionDto[]>> GetAsync(
        [FromQuery] GetTransactionsRequest request)
    {
        var response = await transactionService.GetAsync(request);

        return Ok(response);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<TransactionDto>> GetTransactionByIdAsync(
        [FromRoute] int id)
    {
        var request = new GetTransactionByIdRequest(id);
        var response = await transactionService.GetByIdAsync(request);

        return Ok(response);
    }

    [HttpGet("{id:int:min(1)}/payments")]
    public async Task<ActionResult<TransactionPaymentDto[]>> GetTransactionPaymentsAsync(
        [FromRoute] int id)
    {
        var request = new GetTransactionPaymentsRequest(id);
        var response = await paymentService.GetTransactionPaymentsAsync(request);

        return Ok(response);
    }

    [HttpPost("{id:int:min(1)}/payments")]
    public async Task<ActionResult<CreatePaymentResponse>> CreatePaymentAsync(
        [FromRoute] int id,
        [FromForm] CreateTransactionPaymentRequest request)
    {
        if (id != request.TransactionId)
        {
            return BadRequest($"Route id: {id} does not match with body id: {request.TransactionId}.");
        }

        await transactionPaymentService.CreatePaymentAsync(request);

        return Created();
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CreateTransactionResponse>> PostAsync(
        [FromForm] CreateTransactionRequest request)
    {
        var response = await transactionService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetTransactionByIdAsync),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:int:min(1)}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UpdateTransactionResponse>> PutAsync(
        [FromRoute] int id,
        [FromForm] UpdateTransactionRequest request)
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

        var response = await transactionService.UpdateAsync(request);

        return Ok(response);
    }

    [HttpDelete("{id:int:min(1)}")]
    public async Task<ActionResult> DeleteAsync(
        [FromRoute] int id)
    {
        var request = new DeleteTransactionRequest(id);
        await transactionService.DeleteAsync(request);

        return NoContent();
    }
}
