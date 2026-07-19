using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Payroll;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class PaymentService(
    IApplicationDbContext context,
    IRequestValidator validator,
    IFileService fileService,
    INumberSequenceAllocator allocator) : IPaymentService
{
    // Uploaded payment files land here (originals + thumbnails under their standard sections).
    private const string AttachmentsSubfolder = "payments";

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

        // DR-25: an expense may not overdraw the source wallet (parity with the negative-stock block, rule 20).
        if (request.Direction.ToDomainDirection() == PaymentDirection.Expense)
        {
            await context.EnsureWalletCanCoverAsync(wallet.Id, request.Amount);
        }

        var settledIds = settlements.Select(s => s.TransactionId).ToArray();
        var transactions = await context.Transactions
            .Where(t => settledIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        if (settlements.Any(s => s.Amount <= 0))
        {
            throw new ValidationException("Settlement amount must be greater than zero.");
        }

        // Collapse duplicate rows per transaction so a repeated TransactionId can't overpay or double-apply.
        var settlementsByTransaction = settlements
            .GroupBy(s => s.TransactionId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

        foreach (var (transactionId, amount) in settlementsByTransaction)
        {
            if (!transactions.TryGetValue(transactionId, out var transaction))
            {
                throw new EntityNotFoundException<TransactionRecord>(transactionId);
            }

            // A payment may only settle its own partner's transactions; otherwise partner A settles
            // partner B's debt and both ledgers corrupt.
            if (transaction.PartnerId != request.PartnerId)
            {
                throw new ValidationException(
                    $"Transaction {transactionId} does not belong to partner {request.PartnerId}.");
            }

            if (amount > transaction.UnpaidAmount)
            {
                throw new ValidationException(
                    $"Settlement of {amount} exceeds the remaining {transaction.UnpaidAmount} on transaction {transaction.Id}.");
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
        foreach (var (transactionId, amount) in settlementsByTransaction)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                Payment = payment,
                TransactionId = transactionId,
                Type = PaymentAllocationType.TransactionSettlement,
                Amount = amount,
            });

            transactions[transactionId].AddPayment(amount);
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

        // Allocate the number and insert in one transaction so a rolled-back create leaves no gap (rule 4).
        await using var databaseTransaction = await context.Database.BeginTransactionAsync();

        payment.Number = await allocator.AllocateAsync(NumberSeriesType.Payment);
        context.Payments.Add(payment);
        await AddAttachmentsAsync(request, payment);
        await context.SaveChangesAsync();

        await databaseTransaction.CommitAsync();

        return await GetRecordByIdAsync(payment.Id);
    }

    /// <summary>
    /// Uploads the request's files and links them to the payment, mirroring the transaction attachment flow:
    /// the original name, MIME type, and size are kept so the client can render each without re-reading the file.
    /// </summary>
    private async Task AddAttachmentsAsync(CreatePaymentRecordRequest request, Payment payment)
    {
        if (request.Attachments is not { Length: > 0 })
        {
            return;
        }

        foreach (var file in request.Attachments)
        {
            var uploaded = await fileService.UploadAsync(file, AttachmentsSubfolder);

            payment.Attachments.Add(new PaymentAttachment
            {
                Payment = payment,
                FileId = uploaded.FileName,
                FileName = uploaded.OriginalFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                Url = uploaded.Url,
            });
        }
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

        var walletRows = await context.Wallets
            .Where(w => !w.IsArchived)
            .OrderBy(w => w.Name)
            .Select(w => new { w.Id, w.Name, Type = w.Type.ToString() })
            .ToArrayAsync();

        var wallets = new PaymentFormWalletDto[walletRows.Length];
        for (var i = 0; i < walletRows.Length; i++)
        {
            var w = walletRows[i];
            // Full computed balance incl. payment activity, via the shared calculator — the form must
            // match the wallets list (a payment-only wallet used to show 0 here while the list showed its
            // real, possibly negative, balance).
            var balance = await context.ComputeWalletBalanceAsync(w.Id);
            wallets[i] = new PaymentFormWalletDto(w.Id, w.Name, w.Type, balance);
        }

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
            p.Number.ToString(),
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
                a.Amount)).ToArray(),
            p.Attachments.Select(a => new PaymentAttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                a.Url)).ToArray(),
            // Echo the settled transaction's note (the single one this payment settles; first if several).
            p.Allocations
                .Where(a => a.Type == PaymentAllocationType.TransactionSettlement && a.Transaction != null)
                .Select(a => a.Transaction!.Notes)
                .FirstOrDefault(),
            // Echo the settled transaction(s)' attachments (union), stored on the transaction — not duplicated.
            p.Allocations
                .Where(a => a.Type == PaymentAllocationType.TransactionSettlement && a.Transaction != null)
                .SelectMany(a => a.Transaction!.Attachments)
                .Select(ta => new PaymentAttachmentDto(
                    ta.Id,
                    ta.FileName,
                    ta.ContentType,
                    ta.SizeBytes,
                    ta.Url)).ToArray());

    public async Task<PaymentRecordDto> CreateAsync(CreatePayrollRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId)
            ?? throw new EntityNotFoundException<Employee>(request.EmployeeId);

        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.Id == request.WalletId)
            ?? throw new EntityNotFoundException<Wallet>(request.WalletId);

        // DR-25: payroll always leaves the wallet — it may not overdraw it (parity with negative stock, rule 20).
        await context.EnsureWalletCanCoverAsync(wallet.Id, request.Amount);

        var payment = new Payment
        {
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

        // Allocate the number and insert in one transaction so a rolled-back create leaves no gap (rule 4).
        await using var databaseTransaction = await context.Database.BeginTransactionAsync();

        payment.Number = await allocator.AllocateAsync(NumberSeriesType.Payment);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        await databaseTransaction.CommitAsync();

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
                a.TransactionId!.Value,
                a.PaymentId,
                a.Amount,
                a.Payment.Number.ToString(),
                a.Payment.Wallet != null ? a.Payment.Wallet.Name : null,
                a.Payment.Wallet != null ? a.Payment.Wallet.Type.ToString() : null,
                a.Payment.Notes,
                a.Payment.DateUtc))
            .ToArrayAsync();
    }
}
