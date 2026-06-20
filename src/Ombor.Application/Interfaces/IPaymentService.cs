using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Payroll;
using Ombor.Contracts.Responses.Payment;

namespace Ombor.Application.Interfaces;

public interface IPaymentService
{
    Task<TransactionPaymentDto[]> GetTransactionPaymentsAsync(GetTransactionPaymentsRequest request);

    Task<PaymentRecordDto[]> GetRecordsAsync(GetPaymentsRequest request);
    Task<PaymentRecordDto> GetRecordByIdAsync(int id);
    Task<PaymentRecordDto> CreateRecordAsync(CreatePaymentRecordRequest request);
    Task<PaymentFormDataDto> GetFormDataAsync();
    Task<OutstandingTransactionDto[]> GetOutstandingAsync(int partnerId);
    Task<PaymentRecordDto> CreateAsync(CreatePayrollRequest request);
}
