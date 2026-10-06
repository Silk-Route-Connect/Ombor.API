using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Validators;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Extensions;

/// <summary>
/// Shared payment calculations used by both the payment and transaction create flows.
/// </summary>
internal static class PaymentCalculationExtensions
{
    /// <summary>
    /// The transaction types a payment in <paramref name="direction"/> may settle. Income settles what the partner
    /// owes us (Sale, SupplyRefund); Expense settles what we owe the partner (Supply, SaleRefund).
    /// </summary>
    public static TransactionType[] SettlableTypes(this PaymentDirection direction) =>
        direction == PaymentDirection.Income
            ? [TransactionType.Sale, TransactionType.SupplyRefund]
            : [TransactionType.Supply, TransactionType.SaleRefund];

    /// <summary>
    /// Rejects a settlement whose transaction runs the other way (an Income "paying off" a supplier debt would count
    /// the money twice in our favour). Owner decision 2026-10-04 — narrows the DR-05 deferral to this obviously-wrong
    /// case; allocation ordering stays deferred.
    /// </summary>
    public static void EnsureSettlableBy(this TransactionRecord transaction, PaymentDirection direction, string propertyName)
    {
        if (!direction.SettlableTypes().Contains(transaction.Type))
        {
            throw CodedValidation.Failure(
                propertyName,
                $"A {direction} payment cannot settle {transaction.Type} transaction {transaction.Id}.",
                ErrorCodes.PaymentDirectionMismatch);
        }
    }

    /// <summary>
    /// Sums the partner's unpaid debt that a payment in the given direction can settle (rule 40 gate).
    /// </summary>
    public static async Task<decimal> ComputeSettlableDebtAsync(
        this IApplicationDbContext context,
        int partnerId,
        PaymentDirection direction)
    {
        var settlableTypes = direction.SettlableTypes();

        return await context.Transactions
            .Where(t => t.PartnerId == partnerId
                && settlableTypes.Contains(t.Type)
                && t.TotalDue > t.TotalPaid)
            .SumAsync(t => (decimal?)(t.TotalDue - t.TotalPaid)) ?? 0m;
    }
}
