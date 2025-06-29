using Ombor.Contracts.Requests.Payments;

namespace Ombor.Application.Interfaces;

public interface ITransactionPaymentService
{
    Task CreatePaymentAsync(CreateTransactionPaymentRequest request);
}