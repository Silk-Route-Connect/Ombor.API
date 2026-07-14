using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
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

    /// <summary>
    /// Persists a newly added payment, minting its sequential number and retrying if a concurrent
    /// create claimed the same value. The number is the dispute trail's human handle and is guarded by a
    /// unique index on (OrganizationId, Number); a race must never surface as a duplicate or a 500.
    /// </summary>
    /// <remarks>
    /// Deliberately runs without an explicit transaction: one <see cref="IApplicationDbContext.SaveChangesAsync"/>
    /// is already atomic, so a failed attempt fully rolls back before the next number is minted. Wrapping this
    /// in a manual transaction would replay partially-applied statements on retry.
    /// </remarks>
    public static async Task SaveWithPaymentNumberAsync(this IApplicationDbContext context, Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        const int maxAttempts = 5;

        for (var attempt = 1; ; attempt++)
        {
            payment.Number = await context.NextPaymentNumberAsync();

            try
            {
                await context.SaveChangesAsync();
                return;
            }
            catch (DbUpdateException) when (attempt < maxAttempts)
            {
                // A concurrent payment claimed this number; re-mint against the now-committed count and retry.
            }
        }
    }
}
