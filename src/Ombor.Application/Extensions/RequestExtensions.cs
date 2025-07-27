using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Application.Extensions;

internal static class RequestExtensions
{
    public static CreateTransactionPaymentRequest ToPaymentRequest(this CreateTransactionRequest request, int transactionId)
    {
        return new CreateTransactionPaymentRequest(
            transactionId,
            request.Notes,
            request.ShouldReturnChange,
            request.Attachments,
            request.Payments,
            request.DebtPayments);
    }
}
