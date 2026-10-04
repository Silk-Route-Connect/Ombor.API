using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Payroll;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class PaymentService(
    IApplicationDbContext context,
    IRequestValidator validator,
    IFileService fileService,
    INumberSequenceAllocator allocator,
    PaymentQueries queries) : IPaymentService
{
    // Uploaded payment files land here (originals + thumbnails under their standard sections).
    private const string AttachmentsSubfolder = "payments";

    public async Task<PaymentRecordDto> CreateRecordAsync(CreatePaymentRecordRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var settlements = request.Settlements ?? [];

        await OwnedReferences.Check()
            .Require(context.Wallets, request.WalletId, nameof(request.WalletId))
            .Require(context.Partners, request.PartnerId, nameof(request.PartnerId))
            .Require(context.Employees, request.EmployeeId, nameof(request.EmployeeId))
            .Require(context.Transactions, settlements.Select((s, i) => (s.TransactionId, $"Settlements[{i}].TransactionId")))
            .ThrowIfMissingAsync();

        var direction = request.Direction.ToDomainDirection();
        var settlementTotal = settlements.Sum(s => s.Amount);

        if (settlementTotal > request.Amount)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.Settlements), "Settlements cannot exceed the payment amount.")]);
        }

        // DR-25: an expense may not overdraw the source wallet (parity with the negative-stock block, rule 20).
        if (direction == PaymentDirection.Expense)
        {
            await context.EnsureWalletCanCoverAsync(request.WalletId, request.Amount, nameof(request.Amount));
        }

        var settlementsByTransaction = await ValidateSettlementsAsync(request, settlements, direction);
        var advanceAmount = request.Amount - settlementTotal;

        // Advance is only allowed once the partner has no debt left in this payment's direction (rule 40).
        if (advanceAmount > 0 && request.PartnerId is int debtPartnerId)
        {
            var remainingDebt = await context.ComputeSettlableDebtAsync(debtPartnerId, direction) - settlementTotal;

            if (remainingDebt > 0)
            {
                throw new ValidationException(
                    [new ValidationFailure(nameof(request.Amount), "Cannot create an advance while the partner still has outstanding debt.")]);
            }
        }

        var type = request.Type.ToDomainType();
        var payment = new Payment
        {
            Type = type,
            Direction = direction,
            DateUtc = DateTimeOffset.UtcNow,
            PartnerId = request.PartnerId,
            EmployeeId = request.EmployeeId,
            WalletId = request.WalletId,
            Notes = request.Description,
        };

        // A payroll paid from the payments module records the same facts as one paid from the employee page:
        // the period it covers and the salary at that moment (a later salary change must not rewrite history).
        if (type == PaymentType.Payroll && request.EmployeeId is int employeeId)
        {
            payment.Period = request.Period;
            payment.Salary = await context.Employees
                .Where(e => e.Id == employeeId)
                .Select(e => (decimal?)e.Salary)
                .FirstOrDefaultAsync();
        }

        // Source side (rule 9): the whole amount moves through one wallet.
        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = request.WalletId,
            Amount = request.Amount,
        });

        // Settling allocations (rule 10): one per settled transaction, plus any excess parked as advance.
        foreach (var (settled, amount) in settlementsByTransaction)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                Payment = payment,
                TransactionId = settled.Id,
                Type = PaymentAllocationType.TransactionSettlement,
                Amount = amount,
            });

            settled.AddPayment(amount);
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

    public async Task<PaymentRecordDto> CreateAsync(CreatePayrollRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        await OwnedReferences.Check()
            .Require(context.Employees, request.EmployeeId, nameof(request.EmployeeId))
            .Require(context.Wallets, request.WalletId, nameof(request.WalletId))
            .ThrowIfMissingAsync();

        var employee = await context.Employees.FirstAsync(e => e.Id == request.EmployeeId);

        // DR-25: payroll always leaves the wallet — it may not overdraw it (parity with negative stock, rule 20).
        await context.EnsureWalletCanCoverAsync(request.WalletId, request.Amount, nameof(request.Amount));

        var payment = new Payment
        {
            Type = PaymentType.Payroll,
            Direction = PaymentDirection.Expense,
            DateUtc = DateTimeOffset.UtcNow,
            EmployeeId = employee.Id,
            WalletId = request.WalletId,
            Period = request.Period,
            Salary = employee.Salary, // snapshot at creation so a later salary change doesn't rewrite history
            Notes = request.Notes,
        };

        // Source side (rule 9): payroll is money leaving one wallet.
        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = request.WalletId,
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

    public Task<PaymentRecordDto[]> GetRecordsAsync(GetPaymentsRequest request) => queries.GetRecordsAsync(request);

    public Task<PaymentRecordDto> GetRecordByIdAsync(int id) => queries.GetRecordByIdAsync(id);

    public Task<PaymentFormDataDto> GetFormDataAsync() => queries.GetFormDataAsync();

    public Task<OutstandingTransactionDto[]> GetOutstandingAsync(int partnerId) => queries.GetOutstandingAsync(partnerId);

    public Task<TransactionPaymentDto[]> GetTransactionPaymentsAsync(GetTransactionPaymentsRequest request)
        => queries.GetTransactionPaymentsAsync(request);

    /// <summary>
    /// Each settled transaction must be this partner's, run in this payment's direction, and still owe at least the
    /// amount applied. Duplicate rows per transaction are summed first so a repeated id can't overpay.
    /// </summary>
    private async Task<List<(TransactionRecord Transaction, decimal Amount)>> ValidateSettlementsAsync(
        CreatePaymentRecordRequest request,
        SettlementInput[] settlements,
        PaymentDirection direction)
    {
        var settledIds = settlements.Select(s => s.TransactionId).ToArray();
        var transactions = await context.Transactions
            .Where(t => settledIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        var grouped = settlements
            .Select((s, index) => (s.TransactionId, s.Amount, Index: index))
            .GroupBy(s => s.TransactionId)
            .Select(g => (TransactionId: g.Key, Amount: g.Sum(s => s.Amount), Property: $"Settlements[{g.First().Index}].TransactionId"));

        var result = new List<(TransactionRecord, decimal)>();

        foreach (var (transactionId, amount, property) in grouped)
        {
            var transaction = transactions[transactionId];

            // A payment may only settle its own partner's transactions; otherwise partner A settles
            // partner B's debt and both ledgers corrupt.
            if (transaction.PartnerId != request.PartnerId)
            {
                throw new ValidationException(
                    [new ValidationFailure(property, $"Transaction {transactionId} does not belong to partner {request.PartnerId}.")]);
            }

            transaction.EnsureSettlableBy(direction, property);

            if (amount > transaction.UnpaidAmount)
            {
                throw new ValidationException(
                    [new ValidationFailure(property, $"Settlement of {amount} exceeds the remaining {transaction.UnpaidAmount} on transaction {transaction.Id}.")]);
            }

            result.Add((transaction, amount));
        }

        return result;
    }

    /// <summary>
    /// Uploads the request's files and links them to the payment, mirroring the transaction attachment flow:
    /// the original name, server-detected MIME type, and size are kept so the client can render each without
    /// re-reading the file.
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
                ContentType = uploaded.ContentType,
                SizeBytes = file.Length,
                Url = uploaded.Url,
            });
        }
    }
}
