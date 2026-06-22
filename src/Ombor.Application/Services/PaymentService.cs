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
    public async Task<PaymentRecordDto> CreateRecordAsync(CreatePaymentRecordRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.Id == request.WalletId)
            ?? throw new EntityNotFoundException<Wallet>(request.WalletId);

        if (request.PartnerId is int partnerId &&
            !await context.Partners.AnyAsync(p => p.Id == partnerId))
        {
            throw new EntityNotFoundException<Partner>(partnerId);
        }

        if (request.EmployeeId is int employeeId &&
            !await context.Employees.AnyAsync(e => e.Id == employeeId))
        {
            throw new EntityNotFoundException<Employee>(employeeId);
        }

        var settlements = request.Settlements ?? [];
        var settlementTotal = settlements.Sum(s => s.Amount);

        if (settlementTotal > request.Amount)
        {
            throw new ValidationException("Settlements cannot exceed the payment amount.");
        }

        var settledIds = settlements.Select(s => s.TransactionId).ToArray();
        var transactions = await context.Transactions
            .Where(t => settledIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        foreach (var settlement in settlements)
        {
            if (settlement.Amount <= 0)
            {
                throw new ValidationException("Settlement amount must be greater than zero.");
            }

            if (!transactions.TryGetValue(settlement.TransactionId, out var transaction))
            {
                throw new EntityNotFoundException<TransactionRecord>(settlement.TransactionId);
            }

            if (settlement.Amount > transaction.UnpaidAmount)
            {
                throw new ValidationException(
                    $"Settlement of {settlement.Amount} exceeds the remaining {transaction.UnpaidAmount} on transaction {transaction.Id}.");
            }
        }

        var advanceAmount = request.Amount - settlementTotal;

        // Advance is only allowed once the partner has no debt left in this payment's direction (rule 40).
        if (advanceAmount > 0 && request.PartnerId is int debtPartnerId)
        {
            var direction = request.Direction.ToDomainDirection();
            var remainingDebt = await context.ComputeSettlableDebtAsync(debtPartnerId, direction) - settlementTotal;

            if (remainingDebt > 0)
            {
                throw new ValidationException(
                    "Cannot create an advance while the partner still has outstanding debt.");
            }
        }

        var payment = new Payment
        {
            Number = await context.NextPaymentNumberAsync(),
            Type = request.Type.ToDomainType(),
            Direction = request.Direction.ToDomainDirection(),
            DateUtc = DateTimeOffset.UtcNow,
            PartnerId = request.PartnerId,
            EmployeeId = request.EmployeeId,
            WalletId = wallet.Id,
            Notes = request.Description,
        };

        // Source side (rule 9): the whole amount moves through one wallet.
        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = wallet.Id,
            Amount = request.Amount,
        });

        // Settling allocations (rule 10): one per settled transaction, plus any excess parked as advance.
        foreach (var settlement in settlements)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                Payment = payment,
                TransactionId = settlement.TransactionId,
                Type = PaymentAllocationType.TransactionSettlement,
                Amount = settlement.Amount,
            });

            transactions[settlement.TransactionId].AddPayment(settlement.Amount);
        }

        if (advanceAmount > 0 && request.PartnerId is not null)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                Payment = payment,
                Type = PaymentAllocationType.AdvanceCredit,
                Amount = advanceAmount,
            });
        }

        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        return await GetRecordByIdAsync(payment.Id);
    }

    public Task<PaymentRecordDto[]> GetRecordsAsync(GetPaymentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return RecordsQuery(request)
            .OrderByDescending(p => p.DateUtc)
            .Select(ToRecordExpression())
            .ToArrayAsync();
    }

    public async Task<PaymentRecordDto> GetRecordByIdAsync(int id)
    {
        var record = await RecordsQuery(new GetPaymentsRequest())
            .Where(p => p.Id == id)
            .Select(ToRecordExpression())
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException<Payment>(id);

        return record;
    }

    public async Task<PaymentFormDataDto> GetFormDataAsync()
    {
        var partners = await context.Partners
            .Where(p => !p.IsArchived)
            .OrderBy(p => p.Name)
            .Join(
                context.PartnerBalances,
                partner => partner.Id,
                balance => balance.PartnerId,
                (partner, balance) => new PaymentFormPartnerDto(
                    partner.Id,
                    partner.Name,
                    partner.Type.ToString(),
                    balance.Total,
                    balance.PartnerAdvance))
            .ToArrayAsync();

        var employees = await context.Employees
            .OrderBy(e => e.FullName)
            .Select(e => new PaymentFormEmployeeDto(e.Id, e.FullName, e.Position, e.Salary))
            .ToArrayAsync();

        var wallets = await context.Wallets
            .Where(w => !w.IsArchived)
            .OrderBy(w => w.Name)
            .Select(w => new PaymentFormWalletDto(
                w.Id,
                w.Name,
                w.Type.ToString(),
                w.OpeningBalance
                    + (w.IncomingTransfers.Sum(t => (decimal?)t.Amount) ?? 0m)
                    - (w.OutgoingTransfers.Sum(t => (decimal?)t.Amount) ?? 0m)))
            .ToArrayAsync();

        return new PaymentFormDataDto(partners, employees, wallets);
    }

    public Task<OutstandingTransactionDto[]> GetOutstandingAsync(int partnerId)
    {
        return context.Transactions
            .Where(t => t.PartnerId == partnerId && t.TotalDue > t.TotalPaid)
            .OrderBy(t => t.DateUtc)
            .Select(t => new OutstandingTransactionDto(
                t.Id,
                t.DateUtc,
                t.Type.ToString(),
                t.TotalDue,
                t.TotalPaid,
                t.TotalDue - t.TotalPaid))
            .ToArrayAsync();
    }

    private IQueryable<Payment> RecordsQuery(GetPaymentsRequest request)
    {
        var query = context.Payments
            .Include(p => p.Partner)
            .Include(p => p.Employee)
            .Include(p => p.Wallet)
            .Include(p => p.Components).ThenInclude(c => c.Wallet)
            .Include(p => p.Allocations)
            .AsNoTracking()
            .AsQueryable();

        if (request.PartnerId.HasValue)
        {
            query = query.Where(p => p.PartnerId == request.PartnerId.Value);
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(p => p.EmployeeId == request.EmployeeId.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(p => p.DateUtc >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(p => p.DateUtc <= request.ToDate.Value);
        }

        if (request.Type.HasValue)
        {
            var type = request.Type.Value.ToDomainType();
            query = query.Where(p => p.Type == type);
        }

        if (request.Direction.HasValue)
        {
            var direction = request.Direction.Value.ToDomainDirection();
            query = query.Where(p => p.Direction == direction);
        }

        return query;
    }

    private static System.Linq.Expressions.Expression<Func<Payment, PaymentRecordDto>> ToRecordExpression() =>
        p => new PaymentRecordDto(
            p.Id,
            p.Number,
            p.DateUtc,
            p.Type.ToString(),
            p.Direction.ToString(),
            p.PartnerId,
            p.Partner != null ? p.Partner.Name : null,
            p.Partner != null ? p.Partner.Type.ToString() : null,
            p.EmployeeId,
            p.Employee != null ? p.Employee.FullName : null,
            p.Employee != null ? p.Employee.Position : null,
            p.WalletId,
            p.Wallet != null ? p.Wallet.Name : null,
            p.Wallet != null ? p.Wallet.Type.ToString() : null,
            p.Components.Sum(c => c.Amount),
            string.Empty,
            p.Notes,
            p.Period,
            p.Salary,
            null,
            p.Components.Select(c => new PaymentSourceDto(
                c.Id,
                c.SourceType.ToString(),
                c.WalletId,
                c.Wallet != null ? c.Wallet.Name : null,
                c.Wallet != null ? c.Wallet.Type.ToString() : null,
                c.Amount)).ToArray(),
            p.Allocations.Select(a => new PaymentAllocationEntryDto(
                a.Id,
                a.Type.ToString(),
                a.TransactionId,
                a.Transaction != null ? a.Transaction.Type.ToString() : null,
                a.Amount)).ToArray());

    public async Task<PaymentRecordDto> CreateAsync(CreatePayrollRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId)
            ?? throw new EntityNotFoundException<Employee>(request.EmployeeId);

        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.Id == request.WalletId)
            ?? throw new EntityNotFoundException<Wallet>(request.WalletId);

        var payment = new Payment
        {
            Number = await context.NextPaymentNumberAsync(),
            Type = PaymentType.Payroll,
            Direction = PaymentDirection.Expense,
            DateUtc = DateTimeOffset.UtcNow,
            EmployeeId = employee.Id,
            WalletId = wallet.Id,
            Period = request.Period,
            Salary = employee.Salary, // snapshot at creation so a later salary change doesn't rewrite history
            Notes = request.Notes,
        };

        // Source side (rule 9): payroll is money leaving one wallet.
        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = wallet.Id,
            Amount = request.Amount,
        });

        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        return await GetRecordByIdAsync(payment.Id);
    }

    public Task<TransactionPaymentDto[]> GetTransactionPaymentsAsync(GetTransactionPaymentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A transaction's payments are its settling allocations, joined to the payment that made them.
        return context.PaymentAllocations
            .Where(a => a.TransactionId == request.TransactionId && a.Type == PaymentAllocationType.TransactionSettlement)
            .OrderByDescending(a => a.Payment.DateUtc)
            .Select(a => new TransactionPaymentDto(
                a.Id,
                request.TransactionId,
                a.Amount,
                a.Payment.Number,
                a.Payment.Wallet != null ? a.Payment.Wallet.Name : null,
                a.Payment.Wallet != null ? a.Payment.Wallet.Type.ToString() : null,
                a.Payment.Notes,
                a.Payment.DateUtc))
            .ToArrayAsync();
    }
}
