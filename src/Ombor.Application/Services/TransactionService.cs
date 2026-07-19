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
    IFileService fileService,
    INumberSequenceAllocator allocator) : ITransactionService
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
                x.Number,
                x.Type,
                x.Status,
                x.DueDate,
                x.PartnerId,
                PartnerName = x.Partner.Name,
                x.DateUtc,
                x.TotalDue,
                x.TotalPaid,
                Lines = x.Lines.Select(l => new TransactionLineDto(l.Id, l.ProductId, l.Product.Name, l.TransactionId, l.UnitPrice, l.Discount, l.DiscountType.ToString(), l.Quantity, l.Total, l.PackageSize)).ToArray(),
                x.OriginalTransactionId,
                OriginalNumber = x.OriginalTransaction != null ? x.OriginalTransaction.Number : null,
                x.RefundReason,
            })
            .ToArrayAsync();

        return [.. rows.Select(x => new TransactionDto(
            x.Id,
            x.Number.ToString(),
            x.PartnerId,
            x.PartnerName,
            x.DateUtc,
            x.Type.ToString(),
            x.Status.ToEffectiveStatusName(x.DueDate, today),
            x.TotalDue,
            x.TotalPaid,
            x.Lines,
            x.OriginalTransactionId,
            x.OriginalNumber?.ToString(),
            x.RefundReason))];
    }

    public async Task<TransactionDto> GetByIdAsync(GetTransactionByIdRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transaction = await context.Transactions
            .Include(x => x.Partner)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            // Load the original so a refund can serve its original document number.
            .Include(x => x.OriginalTransaction)
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
                t.Number,
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
                OriginalNumber = t.OriginalTransaction != null ? t.OriginalTransaction.Number : null,
                t.RefundReason,
                t.Notes,
                CreatedBy = t.CreatedByUser != null ? t.CreatedByUser.FirstName + " " + t.CreatedByUser.LastName : null,
                Attachments = t.Attachments
                    .Select(a => new TransactionAttachmentDto(a.FileName, a.ContentType, a.SizeBytes, a.Url))
                    .ToArray(),
                Lines = t.Lines.Select(l => new TransactionLineDto(
                    l.Id, l.ProductId, l.Product.Name, l.TransactionId,
                    l.UnitPrice, l.Discount, l.DiscountType.ToString(), l.Quantity, l.Total, l.PackageSize)).ToArray(),
                // Settling allocations only (rule 10), newest-first — same shape as GET /{id}/payments.
                Payments = t.PaymentAllocations
                    .Where(a => a.Type == PaymentAllocationType.TransactionSettlement)
                    .OrderByDescending(a => a.Payment.DateUtc)
                    .Select(a => new TransactionPaymentDto(
                        a.Id,
                        t.Id,
                        a.PaymentId,
                        a.Amount,
                        a.Payment.Number.ToString(),
                        a.Payment.Wallet != null ? a.Payment.Wallet.Name : null,
                        a.Payment.Wallet != null ? a.Payment.Wallet.Type.ToString() : null,
                        a.Payment.Notes, a.Payment.DateUtc)).ToArray(),
            })
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException<TransactionRecord>($"Transaction with ID {request.Id} not found.");

        return new TransactionDetailDto(
            row.Id,
            row.Number.ToString(),
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
            row.OriginalNumber?.ToString(),
            row.RefundReason);
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request)
    {
        await ValidateOrThrowAsync(request);

        // Package-entry lines are resolved to base units server-side from the product's package size (rule 21).
        var packageSizes = await context.LoadPackageSizesAsync(
            request.Lines.Where(l => l.PackageQuantity is > 0).Select(l => l.ProductId));

        var transactionEntity = mapper.ToEntity(request, packageSizes);
        transactionEntity.CreatedById = currentUser.UserId;
        var partner = await context.Partners.FindAsync(request.PartnerId)
                ?? throw new InvalidOperationException($"Partner {request.PartnerId} not found");

        await using var databaseTransaction = await context.Database.BeginTransactionAsync();
        try
        {
            await context.MoveStockAsync(
                request.WarehouseId!.Value,
                request.Type.ToDomainType().ToStockMovement(),
                // Use the resolved entity lines so a package-entry line moves its base-unit quantity, not the raw request value.
                transactionEntity.Lines.Select(l => (l.ProductId, l.Quantity, l.UnitPrice)));
            transactionEntity.Number = await allocator.AllocateAsync(NumberSeriesType.Transaction);
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
            // Load the original so a refund's response carries its original document number.
            transactionEntity.OriginalTransaction = transactionEntity.OriginalTransactionId is int originalId
                ? await context.Transactions.FirstOrDefaultAsync(x => x.Id == originalId)
                : null;

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

        // Allocated inside the create's explicit transaction (stock + money atomicity): the allocator's
        // row lock serializes concurrent payment numbers, and a rollback here releases the number cleanly.
        var payment = new Payment
        {
            Number = await allocator.AllocateAsync(NumberSeriesType.Payment),
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

        // Aggregate this request's lines by product before the cap check. Two lines of the same product in
        // one request would otherwise each pass against the same persisted baseline while their sum exceeds
        // the original quantity (over-refund + over-restock). Mirrors the settlement dedup above.
        var requestedQuantities = request.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        foreach (var (productId, requestedQuantity) in requestedQuantities)
        {
            if (!originalQuantities.TryGetValue(productId, out var originalQuantity))
            {
                throw new ValidationException(
                    $"Product {productId} is not part of the original transaction.");
            }

            alreadyRefunded.TryGetValue(productId, out var refundedQuantity);

            if (refundedQuantity + requestedQuantity > originalQuantity)
            {
                throw new ValidationException(
                    $"Refund quantity for product {productId} exceeds the original transaction quantity.");
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
