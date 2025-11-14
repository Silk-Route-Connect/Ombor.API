using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Payment;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.API.Controllers;

[Route("api/transactions")]
[ApiController]
public class TransactionsController(
    ITransactionService transactionService,
    IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedList<TransactionDto>>> GetAsync(
        [FromQuery] GetTransactionsRequest request)
    {
        var response = await transactionService.GetAsync(request);

        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(response.MetaData));

        return Ok(response);
    }

    [HttpGet("{id:int:min(1)}/payments")]
    public async Task<ActionResult<TransactionPaymentDto[]>> GetPaymentsAsync(int id)
    {
        var request = new GetTransactionPaymentsRequest(id);
        var response = await paymentService.GetTransactionPaymentsAsync(request);

        return Ok(response);
    }

    [HttpGet("{id:int:min(1)}/lines")]
    public async Task<ActionResult<TransactionLineDto[]>> GetLinesAsync(int id)
    {
        var request = new GetTransactionByIdRequest(id);
        var response = await transactionService.GetByIdAsync(request);

        return Ok(response.Lines);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> PostAsync(
        [FromForm] CreateTransactionRequest request)
    {
        var response = await transactionService.CreateAsync(request);

        return Created("", response);
    }
}
