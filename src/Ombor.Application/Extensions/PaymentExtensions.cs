using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class PaymentExtensions
{
    public static bool NeedsAutoAllocation(this Payment payment) =>
        payment.PartnerId is not null &&
        payment.Type is PaymentType.Deposit or PaymentType.Withdrawal or PaymentType.Transaction;

    public static void AddAdvanceAllocation(this Payment payment, decimal amountLocal)
    {
        ArgumentNullException.ThrowIfNull(payment);

        payment.Allocations.Add(new PaymentAllocation
        {
            AppliedAmount = amountLocal,
            Type = PaymentAllocationType.AdvancePayment,
            Payment = payment
        });
    }
}
