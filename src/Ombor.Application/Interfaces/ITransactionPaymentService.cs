using Ombor.Contracts.Requests.Payments;
using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface ITransactionPaymentService
{
    Task CreatePaymentAsync(CreateTransactionPaymentRequest request);
    Task CreatePaymentAsync(CreateTransactionPaymentRequest request, TransactionRecord transaction);
}