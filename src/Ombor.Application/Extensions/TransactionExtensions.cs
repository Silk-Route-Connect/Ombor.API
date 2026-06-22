using Ombor.Application.Services;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class TransactionExtensions
{
    // Provisional display number derived from type + id; real per-type sequences are future work (complexity §L).
    public static string ToProvisionalNumber(this TransactionType type, int id)
    {
        var prefix = type switch
        {
            TransactionType.Sale => "S",
            TransactionType.Supply => "SP",
            TransactionType.SaleRefund => "SR",
            TransactionType.SupplyRefund => "SPR",
            _ => "T",
        };

        return $"{prefix}-{id}";
    }

    // Receivable = the partner owes us (unpaid Sale / SupplyRefund); Payable = we owe (unpaid Supply / SaleRefund).
    // Same split as View_PartnerBalance, so debt/dashboard/transaction figures reconcile (complexity notes §J).
    public static string ToDebtDirection(this TransactionType type) =>
        type is TransactionType.Sale or TransactionType.SupplyRefund ? DebtDirections.Receivable : DebtDirections.Payable;

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
