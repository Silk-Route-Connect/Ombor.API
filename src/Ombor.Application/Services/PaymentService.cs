using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class PaymentService(
    IApplicationDbContext context,
    IRequestValidator validator) : IPaymentService
{
    public async Task<PagedList<PaymentDto>> GetAsync(GetPaymentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery(request);
        query = ApplySort(query, request.SortBy);

        var totalCount = await query.CountAsync();

        var payments = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = payments
            .Select(x => new PaymentDto(
                x.Id,
                x.PartnerId,
                x.Partner?.Name,
                x.Notes,
                x.Allocations.Sum(a => a.Amount),
                x.DateUtc,
                x.Direction.ToString(),
                x.Type.ToString(),
                [.. x.Components.Select(c => new PaymentComponentDto(c.Id, c.Method.ToString(), c.Currency, c.Amount, c.ExchangeRate))],
                [.. x.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.PaymentId, a.TransactionId, a.Amount, a.Type.ToString()))]));

        return PagedList<PaymentDto>.ToPagedList(dtos, totalCount, request.PageNumber, request.PageSize);
    }

    public Task<PaymentDto> CreateAsync(CreatePaymentRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<PaymentDto?> CreateAsync(CreateTransactionPaymentRequest request)
    {
        var transaction = await context.Transactions
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId)
            ?? throw new EntityNotFoundException<TransactionRecord>($"Transaction with id: {request.TransactionId} does not exist.");

        var totalDue = transaction.UnpaidAmount;
        var totalPaid = request.Payments.Sum(p => p.Amount * p.ExchangeRate);
        var debtPaymentAmount = request.DebtPayments?.Sum(d => d.Amount) ?? 0;
        var currentTransactionPayment = Math.Min(totalDue, totalPaid);
        var advanceAmount = Math.Max(totalPaid - (currentTransactionPayment + debtPaymentAmount), 0);

        if (totalPaid <= 0)
        {
            return null;
        }

        var payment = new Payment
        {
            DateUtc = DateTime.UtcNow,
            Direction = transaction.Type.GetPaymentDirection(),
            Notes = request.Notes,
            PartnerId = transaction.PartnerId,
            Type = PaymentType.Transaction,
        };
        payment.Allocations.Add(new PaymentAllocation
        {
            Payment = payment,
            Transaction = transaction,
            Amount = currentTransactionPayment,
            Type = transaction.Type.ToPaymentAllocationType()
        });

        if (debtPaymentAmount > 0)
        {
            var transactionsToPay = request.DebtPayments
                .Select(x => x.TransactionId)
                .ToArray();
            var openTransactionsToPay = await context.Transactions
                .Where(x => transactionsToPay.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            foreach (var debtPayment in request.DebtPayments)
            {
                if (!openTransactionsToPay.TryGetValue(debtPayment.TransactionId, out var transactionToPay))
                {
                    throw new InvalidOperationException(""); // TODO: Change to domain error later.
                }

                if (transactionToPay.Status == TransactionStatus.Closed)
                {
                    throw new InvalidOperationException(""); // TODO: Change to domain error later.
                }

                if (debtPayment.Amount > transactionToPay.UnpaidAmount)
                {
                    throw new InvalidOperationException(); // TODO: Change to domain error later.
                }

                transactionToPay.AddPayment(debtPayment.Amount);
                payment.Allocations.Add(new PaymentAllocation
                {
                    Payment = payment,
                    Transaction = transactionToPay,
                    Amount = debtPayment.Amount,
                    Type = transactionToPay.Type.ToPaymentAllocationType()
                });
            }
        }

        if (advanceAmount > 0)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                Payment = payment,
                Transaction = null, // Advance payment
                Amount = advanceAmount,
                Type = request.ShouldReturnChange ? PaymentAllocationType.ChangeReturn : PaymentAllocationType.AdvancePayment
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

        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        return new PaymentDto(
                payment.Id,
                payment.PartnerId,
                payment.Partner?.Name,
                payment.Notes,
                payment.Allocations.Sum(a => a.Amount),
                payment.DateUtc,
                payment.Direction.ToString(),
                payment.Type.ToString(),
                [.. payment.Components.Select(c => new PaymentComponentDto(c.Id, c.Method.ToString(), c.Currency, c.Amount, c.ExchangeRate))],
                [.. payment.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.PaymentId, a.TransactionId, a.Amount, a.Type.ToString()))]);
    }

    public async Task<TransactionPaymentDto[]> GetTransactionPaymentsAsync(GetTransactionPaymentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payments = await context.Payments
            .Include(x => x.Components)
            .Include(x => x.Allocations)
            .Where(x => x.Type == PaymentType.Transaction && x.Allocations.Any(x => x.TransactionId == request.TransactionId))
            .Select(x => new
            {
                x.Id,
                x.DateUtc,
                x.Direction,
                x.Notes,
                Components = x.Components.Select(
                    c => new PaymentComponentDto(c.Id, c.Method.ToString(), c.Currency, c.Amount, c.ExchangeRate)),
                Allocations = x.Allocations.Where(
                    a => a.TransactionId == request.TransactionId)
                .Select(a => new PaymentAllocationDto(a.Id, a.PaymentId, a.TransactionId, a.Amount, a.Type.ToString()))
            })
            .ToArrayAsync();

        return [.. payments
            .Select(x => new TransactionPaymentDto(
                x.Id,
                request.TransactionId,
                x.Allocations.Sum(a => a.Amount),
                x.Components.First().Currency,
                x.Components.First().Method,
                x.Notes,
                x.DateUtc))];
    }

    private async Task ValidateOrThrowAsync(CreateTransactionRequest request, TransactionRecord transaction)
    {
        ArgumentNullException.ThrowIfNull(request);
        // await validator.ValidateAndThrowAsync(request);

        var partner = await context.Partners
            .FirstOrDefaultAsync(x => x.Id == request.PartnerId)
            ?? throw new InvalidOperationException("Partner does not exist");
        var partnerBalance = await context.PartnerBalances
            .FirstAsync(x => x.PartnerId == request.PartnerId);
        var totalDebt = request.Type == Contracts.Enums.TransactionType.Sale
            ? partnerBalance.PayableDebt
            : partnerBalance.ReceivableDebt;

        var totalDue = transaction.UnpaidAmount;
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

    private IQueryable<Payment> GetQuery(GetPaymentsRequest request)
    {
        var query = context.Payments
            .Include(x => x.Partner)
            .Include(x => x.Components)
            .Include(x => x.Allocations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();

            query = query.Where(x => (x.Notes != null && x.Notes.Contains(searchTerm)) ||
            (x.Partner != null && x.Partner.Name.Contains(searchTerm)));
        }

        if (request.PartnerId.HasValue)
        {
            query = query.Where(x => x.PartnerId == request.PartnerId.Value);
        }

        if (request.TransactionId.HasValue)
        {
            query = query.Where(x => x.Allocations.Any(a => a.TransactionId == request.TransactionId.Value));
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(x => x.Allocations.Sum(a => a.Amount) >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(x => x.Allocations.Sum(a => a.Amount) <= request.MaxAmount.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.DateUtc >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.DateUtc <= request.ToDate.Value);
        }

        if (request.Type.HasValue)
        {
            var domainType = request.Type.Value.ToDomainType();
            query = query.Where(x => x.Type == domainType);
        }

        if (request.Direction.HasValue)
        {
            var domainDirection = request.Direction.Value.ToDomainDirection();
            query = query.Where(x => x.Direction == domainDirection);
        }

        return query;
    }

    private IQueryable<Payment> ApplySort(IQueryable<Payment> query, string? sortBy)
        => sortBy?.ToLower() switch
        {
            "direction_asc" => query.OrderBy(x => x.Direction),
            "direction_desc" => query.OrderByDescending(x => x.Direction),
            "type_asc" => query.OrderBy(x => x.Type),
            "type_desc" => query.OrderByDescending(x => x.Type),
            "date_asc" => query.OrderBy(x => x.DateUtc),
            _ => query.OrderByDescending(x => x.DateUtc)
        };
}
