using Ombor.Contracts.Requests.Payments;
using Ombor.Contracts.Responses.Payment;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto[]> GetAsync(GetPaymentsRequest request);
    Task<TransactionPaymentDto[]> GetTransactionPaymentsAsync(GetTransactionPaymentsRequest request);
    Task<PaymentDto> GetByIdAsync(GetPaymentByIdRequest request);
    Task<CreatePaymentResponse> CreateAsync(CreatePaymentRequest request);
    Task<UpdatePaymentResponse> UpdateAsync(UpdatePaymentRequest request);
    Task DeleteAsync(DeletePaymentRequest request);
}
