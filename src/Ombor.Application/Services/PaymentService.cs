using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Payroll;
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
                payment.EmployeeId,
                payment.Employee?.FullName,
                payment.Notes,
                payment.Allocations.Sum(a => a.Amount),
                payment.DateUtc,
                payment.Direction.ToString(),
                payment.Type.ToString(),
                [.. payment.Components.Select(c => new PaymentComponentDto(c.Id, c.Method.ToString(), c.Currency, c.Amount, c.ExchangeRate))],
                [.. payment.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.PaymentId, a.TransactionId, a.Amount, a.Type.ToString()))]);
    }

    public async Task<PaymentDto?> CreateAsync(CreatePayrollRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToPaymentEntity();

        context.Payments.Add(entity);
        await context.SaveChangesAsync();

        entity = await context.Payments
            .Include(x => x.Employee)
            .FirstAsync(x => x.Id == entity.Id);

        return new PaymentDto(
            entity.Id,
            entity.PartnerId,
            entity.Partner?.Name,
            entity.EmployeeId,
            entity.Employee?.FullName,
            entity.Notes,
            entity.Components.Sum(c => c.Amount * c.ExchangeRate),
            entity.DateUtc,
            entity.Direction.ToString(),
            entity.Type.ToString(),
            [.. entity.Components.Select(c => new PaymentComponentDto(c.Id, c.Method.ToString(), c.Currency, c.Amount, c.ExchangeRate))],
            [.. entity.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.PaymentId, a.TransactionId, a.Amount, a.Type.ToString()))]);
    }

    public async Task<PaymentDto> UpdateAsync(UpdatePayrollRequest request)
    {
        await validator.ValidateAndThrowAsync(request);
        var payment = await context.Payments
            .Include(x => x.Components)
            .Include(x => x.Allocations)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(
                x => x.Id == request.PaymentId &&
                x.EmployeeId == request.EmployeeId &&
                x.Type == PaymentType.Payroll)
            ?? throw new EntityNotFoundException<Payment>($"Payroll payment with id: {request.PaymentId} does not exist.");

        payment.ApplyUpdate(request);
        context.Payments.Update(payment);
        await context.SaveChangesAsync();

        return new PaymentDto(
            payment.Id,
            payment.PartnerId,
            payment.Partner?.Name,
            payment.EmployeeId,
            payment.Employee?.FullName,
            payment.Notes,
            payment.Components.Sum(a => a.Amount * a.ExchangeRate),
            payment.DateUtc,
            payment.Direction.ToString(),
            payment.Type.ToString(),
            [.. payment.Components.Select(c => new PaymentComponentDto(c.Id, c.Method.ToString(), c.Currency, c.Amount, c.ExchangeRate))],
            [.. payment.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.PaymentId, a.TransactionId, a.Amount, a.Type.ToString()))]);
    }

    public async Task DeleteAsync(DeletePayrollRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payment = await context.Payments
            .FirstOrDefaultAsync(
                x => x.Id == request.PaymentId &&
                x.Type == PaymentType.Payroll &&
                x.EmployeeId == request.EmployeeId)
            ?? throw new EntityNotFoundException<Payment>($"Payroll payment with id: {request.PaymentId} does not exist.");

        context.Payments.Remove(payment);
        await context.SaveChangesAsync();
    }

    public async Task<PaymentDto[]> GetAsync(GetPaymentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery(request);
        var payments = await query
            .OrderByDescending(x => x.DateUtc)
            .ToArrayAsync();

        return payments
            .Select(x => new PaymentDto(
                x.Id,
                x.PartnerId,
                x.Partner?.Name,
                x.EmployeeId,
                x.Employee?.FullName,
                x.Notes,
                x.Type == PaymentType.Payroll ? x.Components.Sum(x => x.Amount * x.ExchangeRate) : x.Allocations.Sum(a => a.Amount),
                x.DateUtc,
                x.Direction.ToString(),
                x.Type.ToString(),
                [.. x.Components.Select(c => new PaymentComponentDto(c.Id, c.Method.ToString(), c.Currency, c.Amount, c.ExchangeRate))],
                [.. x.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.PaymentId, a.TransactionId, a.Amount, a.Type.ToString()))]))
            .ToArray();
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

        return payments
            .Select(x => new TransactionPaymentDto(
                x.Id,
                request.TransactionId,
                x.Allocations.Sum(a => a.Amount),
                x.Components.First().Currency,
                x.Components.First().Method,
                x.Notes,
                x.DateUtc))
            .ToArray();
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
            .Include(x => x.Employee)
            .Include(x => x.Components)
            .Include(x => x.Allocations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => (x.Notes != null && x.Notes.Contains(request.SearchTerm)) || (x.Partner != null && x.Partner.Name.Contains(request.SearchTerm)));
        }

        if (request.PartnerId.HasValue)
        {
            query = query.Where(x => x.PartnerId == request.PartnerId.Value);
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == request.EmployeeId.Value);
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
}
