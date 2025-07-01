using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Payments;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class TransactionPaymentService(
    IApplicationDbContext context,
    ICurrencyCalculator currencyCalculator,
    IDateTimeProvider dateTimeProvider,
    IPaymentAllocationService allocationService) : ITransactionPaymentService
{
    public async Task CreatePaymentAsync(CreateTransactionPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount < 0)
        {
            throw new InvalidOperationException("Transaction payment amount must be greater than 0.");
        }

        var transaction = await GetOrThrowTransactionAsync(request.TransactionId);

        if (request.Amount == transaction.UnpaidAmount)
        {
            await HandleSinglePaymentAsync(request, transaction);
            return;
        }

        if (request.Amount > transaction.UnpaidAmount)
        {
            await HandleOverPaymentAsync(request, transaction);
            return;
        }

        if (request.Amount < transaction.UnpaidAmount)
        {
            await HandleUnderPaymentAsync(request, transaction);
        }
    }

    public async Task CreatePaymentAsync(CreateTransactionPaymentRequest request, TransactionRecord transaction)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount < 0)
        {
            throw new InvalidOperationException("Transaction payment amount must be greater than 0.");
        }

        if (request.Amount == transaction.UnpaidAmount)
        {
            await HandleSinglePaymentAsync(request, transaction);
            return;
        }

        if (request.Amount > transaction.UnpaidAmount)
        {
            await HandleOverPaymentAsync(request, transaction);
            return;
        }

        if (request.Amount < transaction.UnpaidAmount)
        {
            await HandleUnderPaymentAsync(request, transaction);
        }
    }

    private async Task HandleSinglePaymentAsync(CreateTransactionPaymentRequest request, TransactionRecord transaction)
    {
        if (request.Amount != transaction.UnpaidAmount)
        {
            throw new InvalidOperationException("Payment amount and unpaid amount should be equal.");
        }

        var amountLocal = currencyCalculator.CalculateLocalAmount(request.Amount, request.ExchangeRate);
        var payment = CreateExplicitPayment(request, transaction, amountLocal);

        transaction.Close();
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
    }

    private async Task HandleOverPaymentAsync(CreateTransactionPaymentRequest request, TransactionRecord transaction)
    {
        if (request.Amount <= transaction.UnpaidAmount)
        {
            throw new InvalidOperationException("Payment amount must be greater than Transaction's unpaid amount.");
        }

        var currentTransactionPayment = CreateExplicitPayment(request, transaction, request.Amount);
        context.Payments.Add(currentTransactionPayment);
        await context.SaveChangesAsync();

        var paidAmount = transaction.UnpaidAmount;
        currentTransactionPayment.AmountLocal -= paidAmount;
        transaction.Close();
        await allocationService.ApplyPayment(currentTransactionPayment);
        currentTransactionPayment.AmountLocal += paidAmount;
        await context.SaveChangesAsync();
    }

    private async Task HandleUnderPaymentAsync(CreateTransactionPaymentRequest request, TransactionRecord transaction)
    {
        if (request.Amount >= transaction.UnpaidAmount)
        {
            throw new InvalidOperationException("Payment amount must be less than Transaction's unpaid amount.");
        }

        // If payment amount is 0, then we should check open transactions that can be closed,
        // or withdraw from the balance of a partner if he has any

        // If payment amount is greater than 0, we should create a payment of this amount,
        // afterwards try to close open transactions and/or withdraw from the balance of a partner
        // if he has any money. Withdraw as much as needed to close the transaction

        if (request.Amount > 0)
        {
            var currentTransactionPayment = CreateExplicitPayment(request, transaction, request.Amount);
            context.Payments.Add(currentTransactionPayment);
            transaction.AddPayment(request.Amount);
            await context.SaveChangesAsync();
        }

        var remainingAmount = transaction.UnpaidAmount;
        var openTransactions = await GetOpenTransactionsAsync(transaction.PartnerId, transaction.Type);
        var withdrawalPayment = new Payment
        {
            Currency = PaymentCurrency.UZS,
            DateUtc = dateTimeProvider.UtcNow,
            ExchangeRate = 1,
            Method = PaymentMethod.AccountBalance,
            Notes = $"Account withdrawal for transaction: {transaction.Id}",
            Type = PaymentType.Transaction,
            Amount = 0,
            AmountLocal = 0,
            Direction = transaction.Type.GetPaymentDirection(),
            ExternalReference = null,
            PartnerId = transaction.PartnerId,
        };

        if (openTransactions.Length > 0)
        {
            foreach (var openTransaction in openTransactions)
            {
                var unpaidAmount = openTransaction.UnpaidAmount;
                var amountToPay = Math.Min(remainingAmount, unpaidAmount);
                openTransaction.AddPayment(amountToPay);
                remainingAmount -= amountToPay;
                withdrawalPayment.Allocations.Add(new PaymentAllocation
                {
                    AppliedAmount = amountToPay,
                    Type = transaction.Type.GetPaymentAllocationType(),
                    Payment = withdrawalPayment,
                    Transaction = transaction,
                });
                transaction.AddPayment(amountToPay);

                if (remainingAmount <= 0)
                {
                    break;
                }
            }

            context.Payments.Add(withdrawalPayment);
        }

        if (remainingAmount > 0 && transaction.Partner.HasAvailableBalance(transaction))
        {
            var amountToPay = Math.Min(remainingAmount, Math.Abs(transaction.Partner.Balance));
            withdrawalPayment.Allocations.Add(new PaymentAllocation
            {
                AppliedAmount = amountToPay,
                Payment = withdrawalPayment,
                Transaction = transaction,
                Type = transaction.Type.GetPaymentAllocationType()
            });
            withdrawalPayment.Amount = amountToPay;
            withdrawalPayment.AmountLocal = amountToPay;
            transaction.AddPayment(amountToPay);
        }

        if (withdrawalPayment.Allocations.Count > 0)
        {
            context.Payments.Add(withdrawalPayment);
            await context.SaveChangesAsync();
        }
    }

    private Task<TransactionRecord[]> GetOpenTransactionsAsync(int partnerId, TransactionType type)
    {
        if (type is TransactionType.Sale or TransactionType.SupplyRefund)
        {
            return context.Transactions
                .Where(x => x.Type == TransactionType.Supply || x.Type == TransactionType.SaleRefund)
                .Where(x => x.PartnerId == partnerId)
                .Where(x => x.Status == TransactionStatus.Open)
                .OrderBy(x => x.DateUtc)
                .ToArrayAsync();
        }

        // Get all transactions where partner owes to the company
        return context.Transactions
            .Where(x => x.Type == TransactionType.Sale || x.Type == TransactionType.SupplyRefund)
            .Where(x => x.PartnerId == partnerId)
            .Where(x => x.Status == TransactionStatus.Open)
            .OrderBy(x => x.DateUtc)
            .ToArrayAsync();
    }

    private Payment CreateExplicitPayment(
        CreateTransactionPaymentRequest request,
        TransactionRecord transaction,
        decimal amountLocal)
    {
        var payment = new Payment()
        {
            Amount = request.Amount,
            AmountLocal = amountLocal,
            Currency = request.Currency.ParseToDomain(),
            Method = request.Method.ParseToDomain(),
            ExchangeRate = request.ExchangeRate,
            Direction = transaction.Type.GetPaymentDirection(),
            Notes = request.Notes,
            Type = PaymentType.Transaction,
            DateUtc = dateTimeProvider.UtcNow,
            PartnerId = transaction.PartnerId
        };

        var allocationAmount = Math.Min(transaction.UnpaidAmount, amountLocal);
        payment.Allocations.Add(new PaymentAllocation
        {
            AppliedAmount = allocationAmount,
            Transaction = transaction,
            Payment = payment,
            Type = transaction.Type.GetPaymentAllocationType()
        });

        return payment;
    }

    private async Task<TransactionRecord> GetOrThrowTransactionAsync(int id) =>
        await context.Transactions
            .Include(t => t.Partner)
            .FirstOrDefaultAsync(t => t.Id == id)
        ?? throw new InvalidOperationException($"Transaction with ID {id} not found.");
}
