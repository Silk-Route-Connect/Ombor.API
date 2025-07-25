using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.API.Controllers;

[Route("api/transactions")]
[ApiController]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TransactionDto[]>> GetAsync(
        [FromQuery] GetTransactionsRequest request)
    {
        var response = await transactionService.GetAsync(request);

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> PostAsync(
        [FromForm] CreateTransactionRequest request)
    {
        var response = await transactionService.CreateAsync(request);

        return Created("", response);
    }
}
