using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

/// <summary>
/// Builds the payment a transaction create pays with (source/allocation model, rules 8–10, 15, 40). Split from
/// <see cref="TransactionService"/>; the caller has already run <see cref="TransactionCreateGuard"/> and holds the
/// organization write lock, whose transaction this joins — so the advance gate and the overdraft check see every
/// earlier write.
/// </summary>
internal sealed class TransactionPaymentBuilder(IApplicationDbContext context, INumberSequenceAllocator allocator)
{
    /// <summary>
    /// The paid amount settles this transaction first, then any <see cref="CreateTransactionRequest.Settlements"/>;
    /// the remainder is parked as an advance (gated by rule 40) or returned as change (rule 15).
    /// </summary>
    public async Task AddPaymentAsync(CreateTransactionRequest request, TransactionRecord transaction, int walletId)
    {
        var direction = request.Type.GetPaymentDirection();
        var settlements = request.Settlements ?? [];
        var settlementsTotal = settlements.Sum(s => s.Amount);

        var settleThis = Math.Min(request.PaidAmount, transaction.TotalDue);
        var excess = request.PaidAmount - settleThis - settlementsTotal;

        if (excess < 0m)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.Settlements), "Cannot settle other transactions while this transaction is not fully paid.")]);
        }

        // Advance is only allowed once the partner has no settlable debt left in this direction (rule 40).
        if (excess > 0m && request.Overpayment == Contracts.Enums.OverpaymentHandling.Advance)
        {
            var remainingDebt = await context.ComputeSettlableDebtAsync(request.PartnerId, direction)
                - settleThis - settlementsTotal;

            if (remainingDebt > 0m)
            {
                throw new ValidationException(
                    [new ValidationFailure(nameof(request.Overpayment), "Cannot create an advance while the partner still has outstanding debt.")]);
            }
        }

        // Allocated inside the create's explicit transaction (stock + money atomicity): the allocator's
        // row lock serializes concurrent payment numbers, and a rollback here releases the number cleanly.
        var payment = new Payment
        {
            Number = await allocator.AllocateAsync(NumberSeriesType.Payment),
            Type = PaymentType.Transaction,
            Direction = direction,
            DateUtc = DateTimeOffset.UtcNow,
            PartnerId = request.PartnerId,
            WalletId = walletId,
            Notes = request.Notes,
        };

        // Settling allocation for this transaction first (rule 10).
        payment.Allocations.Add(new PaymentAllocation
        {
            Payment = payment,
            Transaction = transaction,
            Type = PaymentAllocationType.TransactionSettlement,
            Amount = settleThis,
        });
        transaction.AddPayment(settleThis);

        await AddSettlementsAsync(payment, settlements);

        // Excess: park as advance (gated above) or hand back as change (memo only, excluded from balances — rule 15).
        if (excess > 0m)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                Payment = payment,
                Type = request.Overpayment == Contracts.Enums.OverpaymentHandling.Advance
                    ? PaymentAllocationType.AdvanceCredit
                    : PaymentAllocationType.ChangeReturn,
                Amount = excess,
            });
        }

        // Source side (rule 9): one wallet component. Change is handed straight back, so the wallet only
        // nets the kept amount (rule 15); an advance keeps the whole paid amount parked. Either way the
        // source equals the settling allocations (ChangeReturn excluded), satisfying rule 8 by construction.
        var sourceAmount = request.Overpayment == Contracts.Enums.OverpaymentHandling.Change
            ? request.PaidAmount - excess
            : request.PaidAmount;

        // DR-25: a money-out transaction (Supply / SaleRefund) may not overdraw the source wallet (parity with
        // negative stock, rule 20). This runs inside the create transaction, so a block rolls the stock move back.
        if (direction == PaymentDirection.Expense)
        {
            await context.EnsureWalletCanCoverAsync(walletId, sourceAmount, nameof(request.PaidAmount));
        }

        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = walletId,
            Amount = sourceAmount,
        });

        context.Payments.Add(payment);
    }

    /// <summary>Then any other open transactions named in the request (already validated by the guard).</summary>
    private async Task AddSettlementsAsync(Payment payment, SettlementInput[] settlements)
    {
        if (settlements.Length == 0)
        {
            return;
        }

        var settledIds = settlements.Select(s => s.TransactionId).ToArray();
        var settledTransactions = await context.Transactions
            .Where(t => settledIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        // One settling allocation per transaction; duplicate rows are summed (as the guard validated them).
        var settlementsByTransaction = settlements
            .GroupBy(s => s.TransactionId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

        foreach (var (transactionId, amount) in settlementsByTransaction)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                Payment = payment,
                TransactionId = transactionId,
                Type = PaymentAllocationType.TransactionSettlement,
                Amount = amount,
            });
            settledTransactions[transactionId].AddPayment(amount);
        }
    }
}
