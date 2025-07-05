using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class PaymentAllocationService(IApplicationDbContext context) : IPaymentAllocationService
{
    public async Task ApplyPayment(Payment payment)
    {
        if (!payment.PartnerId.HasValue)
        {
            throw new InvalidOperationException("Cannot generate payment allocations for payment without partner.");
        }

        var partnerId = payment.PartnerId.Value;
        var remaining = payment.AmountLocal;
        var openTransactions = await GetOpenTransactionsAsync(partnerId);

        foreach (var transaction in openTransactions)
        {
            if (remaining <= 0)
            {
                break;
            }

            remaining -= PayOpenTransaction(transaction, payment, remaining);
        }

        if (remaining > 0)
        {
            payment.AddAdvanceAllocation(remaining);
        }

        await context.SaveChangesAsync();
    }

    private Task<List<TransactionRecord>> GetOpenTransactionsAsync(int partnerId) =>
        context.Transactions
        .Where(x => x.PartnerId == partnerId)
        .Where(x => x.Status == TransactionStatus.Open)
        .OrderBy(x => x.DateUtc)
        .AsTracking()
        .ToListAsync();

    private static decimal PayOpenTransaction(
        TransactionRecord transaction,
        Payment payment,
        decimal paymentAmount)
    {
        if (transaction.UnpaidAmount <= 0)
        {
            return 0;
        }

        var applied = Math.Min(paymentAmount, transaction.UnpaidAmount);
        transaction.AddPayment(applied);

        payment.Allocations.Add(new PaymentAllocation
        {
            AppliedAmount = applied,
            Type = transaction.Type.GetPaymentAllocationType(),
            TransactionId = transaction.Id,
            Payment = payment
        });

        return applied;
    }
}
