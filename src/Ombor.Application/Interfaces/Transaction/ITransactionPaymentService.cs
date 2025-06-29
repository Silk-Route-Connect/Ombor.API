using Ombor.Contracts.Requests.Payments;

namespace Ombor.Application.Interfaces.Transaction;

public interface ITransactionPaymentService
{
    Task CreatePaymentAsync(CreateTransactionPaymentRequest request);
}