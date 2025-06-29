using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class TransactionExtensions
{
    public static void AddPayment(this TransactionRecord transaction, decimal amountLocal)
    {
        if (transaction.TotalPaid + amountLocal > transaction.TotalDue)
        {
            throw new InvalidOperationException("Transactions overpayment is not allowed.");
        }

        transaction.TotalPaid += amountLocal;

        if (transaction.TotalPaid == transaction.TotalDue)
        {
            transaction.Status = TransactionStatus.Closed;
        }
    }

    public static void Close(this TransactionRecord transaction)
    {
        transaction.TotalPaid = transaction.TotalDue;
        transaction.Status = TransactionStatus.Closed;
    }

    public static bool IsClosed(this TransactionRecord transaction)
        => transaction.Status == TransactionStatus.Closed
        && transaction.TotalDue == transaction.TotalPaid;

    public static bool IsOpen(this TransactionRecord transaction)
        => transaction.Status == TransactionStatus.Open
        && transaction.TotalDue != transaction.TotalPaid;
}
