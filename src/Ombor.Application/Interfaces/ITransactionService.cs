using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedList<TransactionDto>> GetAsync(GetTransactionsRequest request);
    Task<TransactionDto> GetByIdAsync(GetTransactionByIdRequest request);
    Task<TransactionDto> CreateAsync(CreateTransactionRequest request);
}
