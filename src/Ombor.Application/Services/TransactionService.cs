using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

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
                x.Lines.Select(l => new TransactionLineDto(l.Id, l.ProductId, l.Product.Name, l.TransactionId, l.UnitPrice, l.Discount, l.DiscountType.ToString(), l.Quantity, l.Total)),
                x.OriginalTransactionId,
                x.RefundReason))
            .ToArrayAsync();
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

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request)
    {
        await ValidateOrThrowAsync(request);

        var transactionEntity = mapper.ToEntity(request);
        var partner = await context.Partners.FindAsync(request.PartnerId)
                ?? throw new InvalidOperationException($"Partner {request.PartnerId} not found");

        await using var databaseTransaction = await context.Database.BeginTransactionAsync();
        try
        {
            await UpdateProducts(request);
            context.Transactions.Add(transactionEntity);
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

            foreach (var settlement in settlements)
            {
                var settled = settledTransactions[settlement.TransactionId];
                payment.Allocations.Add(new PaymentAllocation
                {
                    Payment = payment,
                    TransactionId = settlement.TransactionId,
                    Type = PaymentAllocationType.TransactionSettlement,
                    Amount = settlement.Amount,
                });
                settled.AddPayment(settlement.Amount);
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

        if (!request.InventoryId.HasValue)
        {
            throw new ValidationException("A warehouse (InventoryId) is required for the transaction.");
        }

        if (!await context.Inventories.AnyAsync(i => i.Id == request.InventoryId.Value))
        {
            throw new ValidationException($"Warehouse {request.InventoryId} does not exist.");
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

        foreach (var settlement in settlements)
        {
            if (!settledTransactions.TryGetValue(settlement.TransactionId, out var settled))
            {
                throw new ValidationException($"Transaction {settlement.TransactionId} does not exist.");
            }

            if (settled.PartnerId != request.PartnerId)
            {
                throw new ValidationException($"Transaction {settlement.TransactionId} does not belong to partner {request.PartnerId}.");
            }

            if (settlement.Amount > settled.UnpaidAmount)
            {
                throw new ValidationException(
                    $"Settlement of {settlement.Amount} exceeds the remaining {settled.UnpaidAmount} on transaction {settled.Id}.");
            }
        }
    }

    /// <summary>
    /// Applies the transaction's stock movement to <see cref="InventoryItem"/> rows of the
    /// selected warehouse. InventoryItem is the sole source of truth for stock; weighted-average
    /// cost is recomputed on every Supply stock-in. Negative stock is hard-blocked.
    /// </summary>
    private async Task UpdateProducts(CreateTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventoryId = request.InventoryId!.Value;
        var domainType = request.Type.ToDomainType();
        var isStockIn = domainType is TransactionType.Supply or TransactionType.SaleRefund;

        var lines = request.Lines
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(l => l.Quantity),
                IncomingValue = g.Sum(l => l.Quantity * l.UnitPrice),
            })
            .ToArray();
        var productIds = lines.Select(x => x.ProductId).ToArray();

        var items = await context.InventoryItems
            .Where(x => x.InventoryId == inventoryId && productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId);

        foreach (var line in lines)
        {
            items.TryGetValue(line.ProductId, out var item);

            if (isStockIn)
            {
                if (item is null)
                {
                    item = new InventoryItem
                    {
                        InventoryId = inventoryId,
                        ProductId = line.ProductId,
                        Quantity = 0,
                        AverageCost = 0m,
                        Inventory = null!,
                        Product = null!,
                    };
                    context.InventoryItems.Add(item);
                }

                if (domainType == TransactionType.Supply)
                {
                    // Weighted-average cost recompute on stock-in (rules.md #10).
                    var newQuantity = item.Quantity + line.Quantity;
                    item.AverageCost = newQuantity == 0
                        ? 0m
                        : ((item.Quantity * item.AverageCost) + line.IncomingValue) / newQuantity;
                    item.Quantity = newQuantity;
                }
                else
                {
                    // SaleRefund: returned goods re-enter at their existing carrying cost.
                    item.Quantity += line.Quantity;
                }
            }
            else
            {
                // Stock-out: Sale, SupplyRefund. Negative stock is hard-blocked.
                if (item is null || item.Quantity < line.Quantity)
                {
                    throw new ValidationException(
                        $"Insufficient stock for product {line.ProductId} in the selected warehouse.");
                }

                item.Quantity -= line.Quantity;
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
            var domainStatus = request.Status.Value.ToDomainStatus();
            query = query.Where(x => x.Status == domainStatus);
        }

        if (request.Type.HasValue)
        {
            var domainTye = request.Type.Value.ToDomainType();
            query = query.Where(x => x.Type == domainTye);
        }

        return query;
    }
}
