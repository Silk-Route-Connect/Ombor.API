using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.Application.Interfaces;

public interface ITransactionService
{
    Task<TransactionDto[]> GetAsync(GetTransactionsRequest request);
    Task<TransactionDto> CreateAsync(CreateTransactionRequest request);
}
