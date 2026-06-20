using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

/// <summary>
/// Shared payment calculations used by both the payment and transaction create flows.
/// </summary>
internal static class PaymentCalculationExtensions
{
    /// <summary>
    /// Sums the partner's unpaid debt that a payment in the given direction can settle (rule 40 gate).
    /// Income settles what the partner owes us (Sale, SupplyRefund); Expense settles what we owe the
    /// partner (Supply, SaleRefund).
    /// </summary>
    public static async Task<decimal> ComputeSettlableDebtAsync(
        this IApplicationDbContext context,
        int partnerId,
        PaymentDirection direction)
    {
        var settlableTypes = direction == PaymentDirection.Income
            ? new[] { TransactionType.Sale, TransactionType.SupplyRefund }
            : new[] { TransactionType.Supply, TransactionType.SaleRefund };

        return await context.Transactions
            .Where(t => t.PartnerId == partnerId
                && settlableTypes.Contains(t.Type)
                && t.TotalDue > t.TotalPaid)
            .SumAsync(t => (decimal?)(t.TotalDue - t.TotalPaid)) ?? 0m;
    }

    /// <summary>Allocates the next sequential payment number («P-1», «P-2», …).</summary>
    public static async Task<string> NextPaymentNumberAsync(this IApplicationDbContext context)
    {
        var count = await context.Payments.CountAsync();

        return $"P-{count + 1}";
    }
}
