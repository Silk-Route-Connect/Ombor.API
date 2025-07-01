using Microsoft.EntityFrameworkCore;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Transactions;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class TransactionValidator(
    IApplicationDbContext context,
    FileSettings fileSettings,
    string webRootPath) : ValidatorBase(context, fileSettings, webRootPath)
{
    public async Task ValidatePostAsync(CreateTransactionRequest request, CreateTransactionResponse response)
    {
        var createdTransaction = await context.Transactions
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == response.Id);

        Assert.NotNull(createdTransaction);

        TransactionAssertionHelper.AssertEquivalent(request, response);
        TransactionAssertionHelper.AssertEquivalent(request, createdTransaction);
    }

    public async Task ValidatePartnerAsync(CreateTransactionRequest request, Partner partnerBefore)
    {
        var updatedPartner = await context.Partners
            .FirstOrDefaultAsync(x => x.Id == partnerBefore.Id);

        var totalDue = request.CalculateTotalDue();
        decimal expectedAmountAfter = 0;

        if (request.Type == Contracts.Enums.TransactionType.Supply)
        {
            var difference = totalDue - request.TotalPaid;
            if (difference > 0)
            {
                expectedAmountAfter = partnerBefore.Balance - difference;
            }
            else
            {
                expectedAmountAfter = partnerBefore.Balance + difference;
            }
        }

        Assert.NotNull(updatedPartner);
        Assert.Equal(expectedAmountAfter, updatedPartner.Balance);
    }

    public async Task ValidatePaymentAsync(CreateTransactionRequest request, CreateTransactionResponse response, Partner partnerBefore)
    {
        var totalDue = request.CalculateTotalDue();

        if (totalDue == request.TotalPaid)
        {
            await ValidateSinglePayment(response.Id, request);
        }

        if (totalDue < request.TotalPaid)
        {
            await ValidateOverpayment(response.Id, request, partnerBefore);
        }

        if (totalDue > request.TotalPaid)
        {

        }
    }

    private async Task ValidateSinglePayment(int transactionId, CreateTransactionRequest request)
    {
        var payments = await context.Payments
            .Where(x => x.Id == transactionId)
            .Include(x => x.Allocations)
            .ToListAsync();
        var allocations = payments.SelectMany(x => x.Allocations).ToList();

        Assert.Single(payments);
        Assert.Single(allocations);

        var payment = payments.Single();
        var allocation = allocations.Single();

        var totalPaidLocal = request.Currency == Contracts.Enums.PaymentCurrency.UZS
            ? request.TotalPaid
            : request.TotalPaid * request.ExchangeRate;
        var expectedDirection = request.Type == Contracts.Enums.TransactionType.Supply || request.Type == Contracts.Enums.TransactionType.SaleRefund
            ? Domain.Enums.PaymentDirection.Expense
            : Domain.Enums.PaymentDirection.Income;

        Assert.Equal(request.Notes, payment.Notes);
        Assert.Equal(request.TotalPaid, payment.Amount);
        Assert.Equal(totalPaidLocal, payment.AmountLocal);
        Assert.Equal(request.ExchangeRate, payment.ExchangeRate);
        Assert.True(payment.DateUtc >= DateTimeOffset.UtcNow.AddSeconds(-100)); // TODO: use proper helper class
        Assert.Equal(Domain.Enums.PaymentType.Transaction, payment.Type);
        Assert.Equal(request.PaymentMethod.ToString(), payment.Method.ToString());
        Assert.Equal(request.Currency.ToString(), payment.Currency.ToString());
        Assert.Equal(expectedDirection, payment.Direction);
        Assert.Equal(request.PartnerId, payment.PartnerId);
        Assert.Equal(request.Attachments?.Length ?? 0, payment.Attachments.Count);

        Assert.Equal(request.TotalPaid, allocation.AppliedAmount);
        Assert.Equal(transactionId, allocation.TransactionId);
        Assert.Equal(request.Type.ToString(), allocation.Type.ToString()); // TODO: map transaction type to allocation type properly, and add check for advanced

        var ledgerEntries = await context.LedgerEntries
            .Where(x => (x.SourceId == transactionId && x.Source == nameof(TransactionRecord)) ||
                (x.SourceId == payment.Id && x.Source == nameof(Payment)))
            .ToListAsync();
        var transactionEntry = ledgerEntries.Single(x => x.Source == nameof(TransactionRecord) && x.SourceId == transactionId);
        var paymentEntry = ledgerEntries.Single(x => x.Source == nameof(Payment) && x.SourceId == payment.Id);

        Assert.True(transactionEntry.CreatedAtUtc >= DateTimeOffset.UtcNow.AddSeconds(-1)); // TODO: use proper helper class
        Assert.Equal(totalPaidLocal, transactionEntry.AmountLocal);
        Assert.Equal(Domain.Enums.LedgerType.InvoiceCreated, transactionEntry.Type);
        Assert.Equal(Domain.Enums.LedgerType.InvoicePaid, paymentEntry.Type);
    }

    private async Task ValidateOverpayment(int transactionId, CreateTransactionRequest request, Partner partnerBefore)
    {
        var payments = await context.Payments
            .Where(x => x.Id == transactionId)
            .Include(x => x.Allocations)
            .ToListAsync();
        var openTransactions = await context.Transactions
            .Where(x => x.PartnerId == partnerBefore.Id)
            .Where(x => x.Status == Domain.Enums.TransactionStatus.Open)
            .OrderBy(x => x.DateUtc)
            .ToListAsync();

        var totalPaidLocal = request.Currency == Contracts.Enums.PaymentCurrency.UZS
                ? request.TotalPaid
                : request.TotalPaid * request.ExchangeRate;
        var expectedDirection = request.Type == Contracts.Enums.TransactionType.Supply || request.Type == Contracts.Enums.TransactionType.SaleRefund
            ? Domain.Enums.PaymentDirection.Expense
            : Domain.Enums.PaymentDirection.Income;
        var totalDue = request.CalculateTotalDue();

        if (openTransactions.Count == 0)
        {
            Assert.Single(payments);

            var payment = payments.Single();
            var allocations = payment.Allocations;

            Assert.Equal(request.Notes, payment.Notes);
            Assert.Equal(request.TotalPaid, payment.Amount);
            Assert.Equal(totalPaidLocal, payment.AmountLocal);
            Assert.Equal(request.ExchangeRate, payment.ExchangeRate);
            Assert.True(payment.DateUtc >= DateTimeOffset.UtcNow.AddSeconds(-1)); // TODO: use proper helper class
            Assert.Equal(Domain.Enums.PaymentType.Transaction, payment.Type);
            Assert.Equal(request.PaymentMethod.ToString(), payment.Method.ToString());
            Assert.Equal(request.Currency.ToString(), payment.Currency.ToString());
            Assert.Equal(expectedDirection, payment.Direction);
            Assert.Equal(request.PartnerId, payment.PartnerId);
            Assert.Equal(request.Attachments?.Length ?? 0, payment.Attachments.Count);

            Assert.Equal(2, allocations.Count);
            var transactionAllocion = allocations.Single(x => x.Type.ToString() == request.Type.ToString());
            var advancePaymentAllocation = allocations.Single(x => x.Type == Domain.Enums.PaymentAllocationType.AdvancePayment);
            var expectedAdvancePaymentAmount = request.TotalPaid - totalDue;

            Assert.Equal(transactionId, transactionAllocion.TransactionId);
            Assert.Equal(totalDue, transactionAllocion.AppliedAmount);

            Assert.Equal(expectedAdvancePaymentAmount, advancePaymentAllocation.AppliedAmount);
            Assert.Null(advancePaymentAllocation.TransactionId);

            return;
        }

        var allocations1 = payments.SelectMany(x => x.Allocations);
        var overpaymentAmount = totalPaidLocal - totalDue;
        var transactionAllocation = allocations1.Single(x => x.TransactionId == transactionId);

        // check transaction allocation
        Assert.Equal(totalDue, transactionAllocation.AppliedAmount);
        Assert.Equal(request.Type.ToString(), transactionAllocation.Type.ToString());
    }

    private async Task ValidateUnderpayment()
    {

    }
}
