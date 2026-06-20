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
            var remainingDebt = await ComputeSettlableDebtAsync(debtPartnerId, direction) - settlementTotal;

            if (remainingDebt > 0)
            {
                throw new ValidationException(
                    "Cannot create an advance while the partner still has outstanding debt.");
            }
        }

        var payment = new Payment
        {
            Number = await NextPaymentNumberAsync(),
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
            null,
            null,
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
                a.TransactionId != null ? "#" + a.TransactionId : a.Type.ToString(),
                a.Amount)).ToArray());

    private async Task<decimal> ComputeSettlableDebtAsync(int partnerId, PaymentDirection direction)
    {
        var settlableTypes = direction == PaymentDirection.Income
            ? new[] { TransactionType.Sale, TransactionType.SupplyRefund }
            : new[] { TransactionType.Supply, TransactionType.SaleRefund };

        return await context.Transactions
            .Where(t => t.PartnerId == partnerId
                && settlableTypes.Contains(t.Type)
                && t.TotalDue > t.TotalPaid)
            .SumAsync(t => (decimal?)(t.TotalDue - t.TotalPaid)) ?? 0m;
    }

    private async Task<string> NextPaymentNumberAsync()
    {
        var count = await context.Payments.CountAsync();

        return $"P-{count + 1}";
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

    public async Task<PaymentDto> CreateAsync(CreatePayrollRequest request)
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

    public async Task<PaymentDto> GetByIdAsync(int id)
    {
        var payment = await context.Payments
            .Include(x => x.Partner)
            .Include(x => x.Employee)
            .Include(x => x.Components)
            .Include(x => x.Allocations)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException<Payment>(id);

        return new PaymentDto(
            payment.Id,
            payment.PartnerId,
            payment.Partner?.Name,
            payment.EmployeeId,
            payment.Employee?.FullName,
            payment.Notes,
            payment.Type == PaymentType.Payroll
                ? payment.Components.Sum(c => c.Amount * c.ExchangeRate)
                : payment.Allocations.Sum(a => a.Amount),
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
