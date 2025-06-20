using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

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
        var partner = await GetPartnerAsync(partnerId);
        var openTransactions = await GetOpenTransactionAsync(partnerId);

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
            AddAdvanceSlice(payment, remaining);
        }

        partner.ApplyPayment(payment);
    }

    private async Task<Partner> GetPartnerAsync(int partnerId) =>
        await context.Partners.FirstOrDefaultAsync(x => x.Id == partnerId)
        ?? throw new EntityNotFoundException<Partner>(partnerId);

    private Task<List<TransactionRecord>> GetOpenTransactionAsync(int partnerId) =>
        context.Transactions
        .Where(x => x.PartnerId == partnerId)
        .Where(x => x.Status == TransactionStatus.Open)
        .OrderBy(x => x.DateUtc)
        .AsTracking()
        .ToListAsync();

    private static decimal PayOpenTransaction(TransactionRecord transaction, Payment payment, decimal paymentAmount)
    {
        var debt = transaction.TotalDue - transaction.TotalPaid;
        var applied = Math.Min(paymentAmount, debt);

        transaction.TotalPaid += applied;
        transaction.Status = transaction.TotalPaid >= transaction.TotalDue
            ? TransactionStatus.Open
            : TransactionStatus.Closed;
        transaction.Status = transaction.TotalPaid >= transaction.TotalDue
            ? TransactionStatus.Open
            : TransactionStatus.Closed;
        transaction.Status = transaction.TotalPaid >= transaction.TotalDue
            ? TransactionStatus.Closed
            : TransactionStatus.Open;

        payment.Allocations.Add(new PaymentAllocation
        {
            AppliedAmount = applied,
            Type = transaction.Type.ToAllocationType(),
            TransactionId = transaction.Id,
            Payment = payment,
        });

        return applied;            // amount that was applied
    }

    private static void AddAdvanceSlice(Payment payment, decimal advance)
    {
        payment.Allocations.Add(new PaymentAllocation
        {
            AppliedAmount = advance,
            Type = PaymentAllocationType.AdvancePayment,
            Payment = payment
        });
    }
}
