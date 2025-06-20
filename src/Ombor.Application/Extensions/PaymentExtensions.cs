using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class PaymentExtensions
{
    public static bool NeedsAutoAllocation(this Payment p) =>
        p.PartnerId is not null &&
        p.Type is PaymentType.Deposit or PaymentType.Withdrawal or PaymentType.Transaction;
}
