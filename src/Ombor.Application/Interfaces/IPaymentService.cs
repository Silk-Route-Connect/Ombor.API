using Ombor.Contracts.Requests.Payments;
using Ombor.Contracts.Responses.Payment;

namespace Ombor.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto[]> GetAsync(GetPaymentsRequest request);
    Task<PaymentDto> GetByIdAsync(GetPaymentByIdRequest request);
    Task<CreatePaymentResponse> CreateAsync(CreatePaymentRequest request);
    Task<UpdatePaymentResponse> UpdateAsync(UpdatePaymentRequest request);
    Task DeleteAsync(DeletePaymentRequest request);
}
