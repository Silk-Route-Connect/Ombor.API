using Ombor.Domain.Entities;

namespace Ombor.Application.Extensions;

internal static class TransactionExtensions
{
    public static void AddPayment(this TransactionRecord transaction, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (amount < 0)
        {
            return;
        }

        if (transaction.UnpaidAmount < amount)
        {
            throw new InvalidOperationException($"Cannot add payment with amount more than unpaid amount. Unpaid amount: {transaction.UnpaidAmount}, payment amount: {amount}");
        }

        transaction.TotalPaid += amount;

        if (transaction.UnpaidAmount > 0)
        {
            transaction.Status = Domain.Enums.TransactionStatus.PartiallyPaid;
        }
        else
        {
            transaction.Status = Domain.Enums.TransactionStatus.Closed;
        }
    }
}
