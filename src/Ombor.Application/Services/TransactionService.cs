using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Payment;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class TransactionService(
    IApplicationDbContext context,
    ITransactionMapper mapper,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser,
    IFileService fileService) : ITransactionService
{
    // Uploaded transaction files land here (originals + thumbnails under their standard sections).
    private const string AttachmentsSubfolder = "transactions";


    public async Task<TransactionDto[]> GetAsync(GetTransactionsRequest request)
    {
        var query = GetQuery(request);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // The served number and the due-date-driven Overdue status are plain C# (untranslatable to SQL), so
        // project the raw columns at the DB then map in memory — the shape DebtService already uses.
        var rows = await query
            .OrderByDescending(x => x.DateUtc)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.Status,
                x.DueDate,
                x.PartnerId,
                PartnerName = x.Partner.Name,
                x.DateUtc,
                x.TotalDue,
                x.TotalPaid,
                Lines = x.Lines.Select(l => new TransactionLineDto(l.Id, l.ProductId, l.Product.Name, l.TransactionId, l.UnitPrice, l.Discount, l.DiscountType.ToString(), l.Quantity, l.Total)).ToArray(),
                x.OriginalTransactionId,
                x.RefundReason,
            })
            .ToArrayAsync();

        return [.. rows.Select(x => new TransactionDto(
            x.Id,
            x.Type.ToProvisionalNumber(x.Id),
            x.PartnerId,
            x.PartnerName,
            x.DateUtc,
            x.Type.ToString(),
            x.Status.ToEffectiveStatusName(x.DueDate, today),
            x.TotalDue,
            x.TotalPaid,
            x.Lines,
            x.OriginalTransactionId,
            x.Type.ToOriginalProvisionalNumber(x.OriginalTransactionId),
            x.RefundReason))];
    }

    public async Task<TransactionDto> GetByIdAsync(GetTransactionByIdRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transaction = await context.Transactions
            .Include(x => x.Partner)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id)
            ?? throw new EntityNotFoundException<TransactionRecord>($"Transaction with ID {request.Id} not found.");

        return mapper.ToDto(transaction);
    }

    public async Task<TransactionDetailDto> GetDetailByIdAsync(GetTransactionByIdRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var row = await context.Transactions
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new
            {
                t.Id,
                t.Type,
                t.Status,
                t.DateUtc,
                t.DueDate,
                t.PartnerId,
                PartnerName = t.Partner.Name,
                PartnerCompany = t.Partner.CompanyName,
                PartnerType = t.Partner.Type,
                t.WarehouseId,
                WarehouseName = t.Warehouse != null ? t.Warehouse.Name : null,
                t.TotalDue,
                t.TotalPaid,
                t.OriginalTransactionId,
                t.RefundReason,
                t.Notes,
                CreatedBy = t.CreatedByUser != null ? t.CreatedByUser.FirstName + " " + t.CreatedByUser.LastName : null,
                Attachments = t.Attachments
                    .Select(a => new TransactionAttachmentDto(a.FileName, a.ContentType, a.SizeBytes, a.Url))
                    .ToArray(),
                Lines = t.Lines.Select(l => new TransactionLineDto(
                    l.Id, l.ProductId, l.Product.Name, l.TransactionId,
                    l.UnitPrice, l.Discount, l.DiscountType.ToString(), l.Quantity, l.Total)).ToArray(),
                // Settling allocations only (rule 10), newest-first — same shape as GET /{id}/payments.
                Payments = t.PaymentAllocations
                    .Where(a => a.Type == PaymentAllocationType.TransactionSettlement)
                    .OrderByDescending(a => a.Payment.DateUtc)
                    .Select(a => new TransactionPaymentDto(
                        a.Id, t.Id, a.Amount, a.Payment.Number,
                        a.Payment.Wallet != null ? a.Payment.Wallet.Name : null,
                        a.Payment.Wallet != null ? a.Payment.Wallet.Type.ToString() : null,
                        a.Payment.Notes, a.Payment.DateUtc)).ToArray(),
            })
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException<TransactionRecord>($"Transaction with ID {request.Id} not found.");

        return new TransactionDetailDto(
            row.Id,
            row.Type.ToProvisionalNumber(row.Id),
            row.Type.ToString(),
            row.Type.ToDebtDirection(),
            row.Status.ToEffectiveStatusName(row.DueDate, today),
            row.DateUtc,
            row.DueDate,
            row.PartnerId,
            row.PartnerName,
            row.PartnerCompany,
            row.PartnerType.ToString(),
            row.WarehouseId,
            row.WarehouseName,
            row.TotalDue,
            row.TotalPaid,
            row.TotalDue - row.TotalPaid,
            row.Lines,
            row.Payments,
            row.Attachments,
            row.CreatedBy,
            row.Notes,
            row.OriginalTransactionId,
            row.Type.ToOriginalProvisionalNumber(row.OriginalTransactionId),
            row.RefundReason);
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request)
    {
        await ValidateOrThrowAsync(request);

        var transactionEntity = mapper.ToEntity(request);
        transactionEntity.CreatedById = currentUser.UserId;
        var partner = await context.Partners.FindAsync(request.PartnerId)
                ?? throw new InvalidOperationException($"Partner {request.PartnerId} not found");

        await using var databaseTransaction = await context.Database.BeginTransactionAsync();
        try
        {
            await context.MoveStockAsync(
                request.WarehouseId!.Value,
                request.Type.ToDomainType().ToStockMovement(),
                request.Lines.Select(l => (l.ProductId, l.Quantity, l.UnitPrice)));
            context.Transactions.Add(transactionEntity);
            await AddAttachmentsAsync(request, transactionEntity);
            await context.SaveChangesAsync();

            if (request.WalletId is int walletId && request.PaidAmount > 0m)
            {
                await BuildPaymentAsync(request, transactionEntity, walletId);
                await context.SaveChangesAsync();
            }

            await databaseTransaction.CommitAsync();

            transactionEntity.Partner = partner;
            transactionEntity.Lines = await context.TransactionLines
                .Include(x => x.Product)
                .Where(x => x.TransactionId == transactionEntity.Id)
                .ToArrayAsync();

            return mapper.ToDto(transactionEntity);
        }
        catch
        {
            await databaseTransaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Uploads the request's files and links them to the transaction. Reuses the file service's
    /// size/type validation; the original name, MIME type, and size are kept so the client can
    /// render each attachment without re-reading the file.
    /// </summary>
    private async Task AddAttachmentsAsync(CreateTransactionRequest request, TransactionRecord transaction)
    {
        if (request.Attachments is not { Length: > 0 })
        {
            return;
        }

        foreach (var file in request.Attachments)
        {
            var uploaded = await fileService.UploadAsync(file, AttachmentsSubfolder);

            transaction.Attachments.Add(new TransactionAttachment
            {
                Transaction = transaction,
                FileId = uploaded.FileName,
                FileName = uploaded.OriginalFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                Url = uploaded.Url,
            });
        }
    }

    /// <summary>
    /// Builds the payment that settles this transaction (source/allocation model, rules 8–10, 15, 40).
    /// The paid amount settles this transaction first, then any <see cref="CreateTransactionRequest.Settlements"/>;
    /// the remainder is parked as an advance (gated by rule 40) or returned as change (rule 15).
    /// </summary>
    private async Task BuildPaymentAsync(CreateTransactionRequest request, TransactionRecord transaction, int walletId)
    {
        var direction = request.Type.GetPaymentDirection();
        var settlements = request.Settlements ?? [];
        var settlementsTotal = settlements.Sum(s => s.Amount);

        var settleThis = Math.Min(request.PaidAmount, transaction.TotalDue);
        var excess = request.PaidAmount - settleThis - settlementsTotal;

        if (excess < 0m)
        {
            throw new ValidationException("Cannot settle other transactions while this transaction is not fully paid.");
        }

        // Advance is only allowed once the partner has no settlable debt left in this direction (rule 40).
        if (excess > 0m && request.Overpayment == Contracts.Enums.OverpaymentHandling.Advance)
        {
            var remainingDebt = await context.ComputeSettlableDebtAsync(request.PartnerId, direction)
                - settleThis - settlementsTotal;

            if (remainingDebt > 0m)
            {
                throw new ValidationException("Cannot create an advance while the partner still has outstanding debt.");
            }
        }

        // Minted eagerly here rather than via the retry helper: this runs inside the create's explicit
        // transaction (stock + money atomicity), where re-saving after a collision would replay the
        // AddPayment updates. The unique (OrganizationId, Number) index still guarantees no duplicate — a
        // rare concurrent collision rolls the whole transaction back cleanly.
        var payment = new Payment
        {
            Number = await context.NextPaymentNumberAsync(),
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

        // Then any other open transactions named in the request.
        if (settlements.Length > 0)
        {
            var settledIds = settlements.Select(s => s.TransactionId).ToArray();
            var settledTransactions = await context.Transactions
                .Where(t => settledIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id);

            // One settling allocation per transaction; duplicate rows are summed (validated in ValidateOrThrowAsync).
            var settlementsByTransaction = settlements
                .GroupBy(s => s.TransactionId)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

            foreach (var (transactionId, amount) in settlementsByTransaction)
            {
                var settled = settledTransactions[transactionId];
                payment.Allocations.Add(new PaymentAllocation
                {
                    Payment = payment,
                    TransactionId = transactionId,
                    Type = PaymentAllocationType.TransactionSettlement,
                    Amount = amount,
                });
                settled.AddPayment(amount);
            }
        }

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
            await context.EnsureWalletCanCoverAsync(walletId, sourceAmount);
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

    private async Task ValidateOrThrowAsync(CreateTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request);

        if (!request.WarehouseId.HasValue)
        {
            throw new ValidationException("A warehouse (WarehouseId) is required for the transaction.");
        }

        if (!await context.Warehouses.AnyAsync(i => i.Id == request.WarehouseId.Value))
        {
            throw new ValidationException($"Warehouse {request.WarehouseId} does not exist.");
        }

        await ValidateRefundOrThrowAsync(request);

        var partner = await context.Partners
            .FirstOrDefaultAsync(x => x.Id == request.PartnerId)
            ?? throw new InvalidOperationException("Partner does not exist");

        if (!partner.CanHandleTransaction(request.Type))
        {
            throw new ValidationException($"Partner of type: {partner.Type} cannot have transactions of type: {request.Type}");
        }

        if (request.PaidAmount > 0m)
        {
            if (!request.WalletId.HasValue)
            {
                throw new ValidationException("A wallet is required when a payment is made.");
            }

            if (!await context.Wallets.AnyAsync(w => w.Id == request.WalletId.Value))
            {
                throw new ValidationException($"Wallet {request.WalletId} does not exist.");
            }
        }

        var settlements = request.Settlements ?? [];

        if (settlements.Sum(s => s.Amount) > request.PaidAmount)
        {
            throw new ValidationException("Settlements cannot exceed the paid amount.");
        }

        if (settlements.Length == 0)
        {
            return;
        }

        var settledIds = settlements.Select(s => s.TransactionId).ToArray();
        var settledTransactions = await context.Transactions
            .Where(t => settledIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        // Collapse duplicate rows per transaction so a repeated TransactionId can't overpay (or 500 on apply).
        var settlementsByTransaction = settlements
            .GroupBy(s => s.TransactionId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

        foreach (var (transactionId, amount) in settlementsByTransaction)
        {
            if (!settledTransactions.TryGetValue(transactionId, out var settled))
            {
                throw new ValidationException($"Transaction {transactionId} does not exist.");
            }

            if (settled.PartnerId != request.PartnerId)
            {
                throw new ValidationException($"Transaction {transactionId} does not belong to partner {request.PartnerId}.");
            }

            if (amount > settled.UnpaidAmount)
            {
                throw new ValidationException(
                    $"Settlement of {amount} exceeds the remaining {settled.UnpaidAmount} on transaction {settled.Id}.");
            }
        }
    }

    /// <summary>Validates refund-specific rules (rules.md #2-6).</summary>
    private async Task ValidateRefundOrThrowAsync(CreateTransactionRequest request)
    {
        var domainType = request.Type.ToDomainType();
        var isRefund = domainType is TransactionType.SaleRefund or TransactionType.SupplyRefund;

        if (!isRefund)
        {
            if (request.OriginalTransactionId.HasValue)
            {
                throw new ValidationException("OriginalTransactionId is only valid for refund transactions.");
            }

            return;
        }

        if (!request.OriginalTransactionId.HasValue)
        {
            throw new ValidationException("Refund transactions require an OriginalTransactionId.");
        }

        var original = await context.Transactions
            .Include(t => t.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.OriginalTransactionId.Value)
            ?? throw new ValidationException($"Original transaction {request.OriginalTransactionId} not found.");

        // Rule 4: a refund cannot itself be refunded.
        if (original.Type is TransactionType.SaleRefund or TransactionType.SupplyRefund)
        {
            throw new ValidationException("A refund transaction cannot itself be refunded.");
        }

        // Rule 3: refund type must match the original type.
        var expectedOriginalType = domainType == TransactionType.SaleRefund
            ? TransactionType.Sale
            : TransactionType.Supply;

        if (original.Type != expectedOriginalType)
        {
            throw new ValidationException(
                $"{domainType} must reference a {expectedOriginalType} transaction.");
        }

        // Rules 5-6: total refunded quantity per product must not exceed the original quantity.
        var originalQuantities = original.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var alreadyRefunded = await context.Transactions
            .Where(t => t.OriginalTransactionId == original.Id)
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

        foreach (var line in request.Lines)
        {
            if (!originalQuantities.TryGetValue(line.ProductId, out var originalQuantity))
            {
                throw new ValidationException(
                    $"Product {line.ProductId} is not part of the original transaction.");
            }

            alreadyRefunded.TryGetValue(line.ProductId, out var refundedQuantity);

            if (refundedQuantity + line.Quantity > originalQuantity)
            {
                throw new ValidationException(
                    $"Refund quantity for product {line.ProductId} exceeds the original transaction quantity.");
            }
        }
    }

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

        if (request.Status.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var domainStatus = request.Status.Value.ToDomainStatus();

            // Overdue is computed on read (a non-closed transaction past its due date), so it overrides the stored
            // Open/PartiallyPaid value. The filter mirrors that: ?status=Overdue selects those rows, and
            // ?status=Open|PartiallyPaid must exclude the ones now showing as Overdue. Closed is never overdue.
            query = domainStatus switch
            {
                Domain.Enums.TransactionStatus.Overdue => query.Where(x =>
                    x.Status != Domain.Enums.TransactionStatus.Closed && x.DueDate != null && x.DueDate < today),
                Domain.Enums.TransactionStatus.Closed => query.Where(x =>
                    x.Status == Domain.Enums.TransactionStatus.Closed),
                _ => query.Where(x =>
                    x.Status == domainStatus && !(x.DueDate != null && x.DueDate < today)),
            };
        }

        if (request.Type.HasValue)
        {
            var domainTye = request.Type.Value.ToDomainType();
            query = query.Where(x => x.Type == domainTye);
        }

        return query;
    }
}
