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
    TransactionCreateGuard guard,
    TransactionPaymentBuilder paymentBuilder,
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
            x.Number?.ToString(),
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
                        a.Payment.WalletId,
                        a.Payment.Wallet != null ? a.Payment.Wallet.Name : null,
                        a.Payment.Wallet != null ? a.Payment.Wallet.Type.ToString() : null,
                        a.Payment.Notes, a.Payment.DateUtc)).ToArray(),
            })
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException<TransactionRecord>($"Transaction with ID {request.Id} not found.");

        return new TransactionDetailDto(
            row.Id,
            row.Number?.ToString(),
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
        var original = await guard.ValidateAsync(request);

        // Package-entry lines are resolved to base units server-side from the product's package size (rule 21).
        var packageSizes = await context.LoadPackageSizesAsync(
            request.Lines.Where(l => l.PackageQuantity is > 0).Select(l => l.ProductId));

        var transactionEntity = mapper.ToEntity(request, packageSizes);
        transactionEntity.CreatedById = currentUser.UserId;

        if (original is not null)
        {
            TransactionCreateGuard.ApplyOriginalPricing(transactionEntity, original);
        }

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
                await paymentBuilder.AddPaymentAsync(request, transactionEntity, walletId);
                await context.SaveChangesAsync();
            }

            await databaseTransaction.CommitAsync();
        }
        catch
        {
            await databaseTransaction.RollbackAsync();
            throw;
        }

        return await GetByIdAsync(new GetTransactionByIdRequest(transactionEntity.Id));
    }

    /// <summary>
    /// Uploads the request's files and links them to the transaction. Reuses the file service's
    /// size/type validation; the original name, server-detected MIME type, and size are kept so the client can
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
                ContentType = uploaded.ContentType,
                SizeBytes = file.Length,
                Url = uploaded.Url,
            });
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
