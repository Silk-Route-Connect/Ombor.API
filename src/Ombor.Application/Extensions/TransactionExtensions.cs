using Ombor.Application.Services;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class TransactionExtensions
{
    // Receivable = the partner owes us (unpaid Sale / SupplyRefund); Payable = we owe (unpaid Supply / SaleRefund).
    // Same split as View_PartnerBalance, so debt/dashboard/transaction figures reconcile (complexity notes §J).
    public static string ToDebtDirection(this TransactionType type) =>
        type is TransactionType.Sale or TransactionType.SupplyRefund ? DebtDirections.Receivable : DebtDirections.Payable;

    // A non-closed transaction whose due date has passed is Overdue (rule 2). Computed on read so the served
    // status never drifts; never persisted. Overdue overrides Open/PartiallyPaid; a Closed row is never overdue.
    public static bool IsOverdue(this TransactionStatus stored, DateOnly? dueDate, DateOnly today) =>
        stored != TransactionStatus.Closed && dueDate is { } due && due < today;

    // The status name served to clients: the stored value, or "Overdue" when the due date has passed.
    public static string ToEffectiveStatusName(this TransactionStatus stored, DateOnly? dueDate, DateOnly today) =>
        stored.IsOverdue(dueDate, today) ? nameof(TransactionStatus.Overdue) : stored.ToString();

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
