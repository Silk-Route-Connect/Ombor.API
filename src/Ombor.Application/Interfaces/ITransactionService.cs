using Ombor.Contracts.Requests.Transactions;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.Application.Interfaces;

public interface ITransactionService
{
    Task<TransactionDto[]> GetAsync(GetTransactionsRequest request);
    Task<TransactionDto> GetByIdAsync(GetTransactionByIdRequest request);
    Task<CreateTransactionResponse> CreateAsync(CreateTransactionRequest request);
    Task<UpdateTransactionResponse> UpdateAsync(UpdateTransactionRequest request);
    Task DeleteAsync(DeleteTransactionRequest request);
}
