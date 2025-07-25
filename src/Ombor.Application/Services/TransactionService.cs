using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class TransactionService(
    IApplicationDbContext context,
    ITransactionMapper mapper,
    IRequestValidator validator) : ITransactionService
{
    public Task<TransactionDto[]> GetAsync(GetTransactionsRequest request)
    {
        var query = GetQuery(request);

        return query
            .OrderByDescending(x => x.DateUtc)
            .Select(x => new TransactionDto(
                x.Id,
                x.PartnerId,
                x.Partner.Name,
                x.DateUtc,
                x.Type.ToString(),
                x.Status.ToString(),
                x.TotalDue,
                x.TotalPaid,
                x.Lines.Select(l => new TransactionLineDto(l.Id, l.ProductId, l.Product.Name, l.TransactionId, l.UnitPrice, l.Discount, l.Quantity))))
            .ToArrayAsync();
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request)
    {
        await ValidateOrThrowAsync(request);

        var transactionEntity = mapper.ToEntity(request);
        var partner = await context.Partners.FindAsync(request.PartnerId)
                ?? throw new InvalidOperationException($"Partner {request.PartnerId} not found");

        await using var databaseTransaction = await context.Database.BeginTransactionAsync();
        try
        {
            var totalDue = transactionEntity.TotalDue;
            var payment = await BuildPayment(request, transactionEntity);

            context.Transactions.Add(transactionEntity);

            if (payment is not null)
            {
                transactionEntity.TotalPaid = payment.Allocations
                    .Where(a => a.Transaction == transactionEntity)
                    .Sum(a => a.Amount);

                transactionEntity.Status = transactionEntity.TotalPaid == totalDue
                    ? TransactionStatus.Closed
                    : transactionEntity.TotalPaid > 0
                        ? TransactionStatus.PartiallyPaid
                        : TransactionStatus.Open;

                context.Payments.Add(payment);
            }

            await context.SaveChangesAsync();
            await databaseTransaction.CommitAsync();

            transactionEntity.Partner = partner;
            transactionEntity.Lines = await context.TransactionLines.Include(x => x.Product).Where(x => x.TransactionId == transactionEntity.Id).ToArrayAsync();
            return mapper.ToDto(transactionEntity);
        }
        catch
        {
            await databaseTransaction.RollbackAsync();
            throw;
        }
    }

    private async Task<Payment?> BuildPayment(CreateTransactionRequest request, TransactionRecord transaction)
    {
        ArgumentNullException.ThrowIfNull(request);

        var totalDue = transaction.TotalDue;
        var totalPaid = request.Payments.Sum(p => p.Amount * p.ExchangeRate);
        var debtPaymentAmount = request.DebtPayments?.Sum(d => d.Amount) ?? 0m;
        var currentTransactionPayment = Math.Min(totalDue, totalPaid);
        var overpaymentAmount = Math.Max(0, totalPaid - (currentTransactionPayment + debtPaymentAmount));

        if (totalPaid <= 0)
        {
            return null;
        }

        var payment = new Payment
        {
            DateUtc = DateTime.UtcNow,
            Direction = request.Type.GetPaymentDirection(),
            Notes = request.Notes,
            PartnerId = request.PartnerId,
            Type = Domain.Enums.PaymentType.Transaction,
        };

        payment.Allocations.Add(new PaymentAllocation
        {
            Payment = payment,
            Transaction = transaction,
            Amount = currentTransactionPayment,
            Type = transaction.Type.ToPaymentAllocationType()
        });

        if (debtPaymentAmount > 0 && request.DebtPayments is not null)
        {
            var transactionIds = request.DebtPayments
                .Select(x => x.TransactionId)
                .ToArray();
            var openTransactionsToPay = await context.Transactions
                .Where(x => transactionIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            foreach (var debtPayment in request.DebtPayments)
            {
                if (!openTransactionsToPay.TryGetValue(debtPayment.TransactionId, out var transactionToPay))
                {
                    throw new InvalidOperationException(""); // TODO: Change to domain error later.
                }

                if (transactionToPay.Status == Domain.Enums.TransactionStatus.Closed)
                {
                    throw new InvalidOperationException(""); // TODO: Change to domain error later.
                }

                if (debtPayment.Amount > transactionToPay.UnpaidAmount)
                {
                    throw new InvalidOperationException(); // TODO: Change to domain error later.
                }

                transactionToPay.TotalPaid += debtPayment.Amount;
                transactionToPay.Status = transactionToPay.TotalDue == transactionToPay.TotalPaid
                    ? Domain.Enums.TransactionStatus.Closed
                    : Domain.Enums.TransactionStatus.PartiallyPaid;
                payment.Allocations.Add(new PaymentAllocation
                {
                    Payment = payment,
                    Transaction = transactionToPay,
                    Amount = debtPayment.Amount,
                    Type = transactionToPay.Type.ToPaymentAllocationType()
                });
            }
        }

        if (overpaymentAmount > 0)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                Payment = payment,
                Transaction = null, // Advance payment
                Amount = overpaymentAmount,
                Type = PaymentAllocationType.AdvancePayment
            });
        }

        foreach (var component in request.Payments)
        {
            payment.Components.Add(new PaymentComponent
            {
                Payment = payment,
                Amount = component.Amount,
                ExchangeRate = component.ExchangeRate,
                Currency = component.Currency,
                Method = component.Method.ToDomainPaymentMethod(),
            });
        }

        return payment;
    }

    private async Task ValidateOrThrowAsync(CreateTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request);

        var partner = await context.Partners
            .FirstOrDefaultAsync(x => x.Id == request.PartnerId)
            ?? throw new InvalidOperationException("Partner does not exist");
        var partnerBalance = await context.PartnerBalances
            .FirstAsync(x => x.PartnerId == request.PartnerId);
        var totalDebt = request.Type == Contracts.Enums.TransactionType.Sale
            ? partnerBalance.PayableDebt
            : partnerBalance.ReceivableDebt;

        var totalDue = request.Lines.Sum(CalculateLineTotal);
        var totalPaid = request.Payments.Sum(x => x.Amount * x.ExchangeRate);
        var totalPaidDebt = request.DebtPayments?.Sum(x => x.Amount) ?? 0;
        var totalPaidAdvance = totalPaid - totalDue - totalPaidDebt; // if negative, no advance payment

        if (totalPaid < totalDue && totalPaidDebt > 0)
        {
            throw new ValidationException("Debt payment is not allowed without fully covering current debt.");
        }

        if (totalDebt < totalPaidDebt)
        {
            throw new ValidationException("Debt payment cannot be greater than partner's total debt amount.");
        }

        if (totalDebt > totalPaidDebt && totalPaidAdvance > 0)
        {
            throw new ValidationException("Cannot make advance payment without closing existing debts.");
        }

        if (!partner.CanHandleTransaction(request.Type))
        {
            throw new ValidationException($"Partner of type: {partner.Type} cannot have transactions of type: {request.Type}");
        }

        var creditRequired = request.Payments
                .Where(p => p.Method == Contracts.Enums.PaymentMethod.AccountBalance)
                .Sum(p => p.Amount * p.ExchangeRate);

        if (creditRequired <= 0)
        {
            return;
        }

        if (request.Type.GetPaymentDirection() == PaymentDirection.Income && partnerBalance.PartnerAdvance < creditRequired)
        {
            throw new ValidationException("Insufficient partner advance balance.");
        }

        if (request.Type.GetPaymentDirection() == PaymentDirection.Expense && partnerBalance.CompanyAdvance < creditRequired)
        {
            throw new ValidationException("Insufficient company advance balance.");
        }
    }

    private static decimal CalculateLineTotal(CreateTransactionLine l)
        => l.UnitPrice * l.Quantity * (1 - (l.Discount / 100m));

    private IQueryable<TransactionRecord> GetQuery(GetTransactionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Transactions
            .Include(x => x.Partner)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .IgnoreAutoIncludes()
            .AsNoTracking();

        if (request.PartnerId.HasValue)
        {
            query = query.Where(x => x.PartnerId == request.PartnerId.Value);
        }

        if (request.Statuses?.Length > 0)
        {
            var domainStatuses = request.Statuses.Select(x => x.ToDomainStatus());
            query = query.Where(x => domainStatuses.Contains(x.Status));
        }

        if (request.Type.HasValue)
        {
            var domainTye = request.Type.Value.ToDomainType();
            query = query.Where(x => x.Type == domainTye);
        }

        return query;
    }
}
