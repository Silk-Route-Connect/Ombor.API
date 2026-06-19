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
    IPaymentService paymentService,
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
                x.Lines.Select(l => new TransactionLineDto(l.Id, l.ProductId, l.Product.Name, l.TransactionId, l.UnitPrice, l.Discount, l.Quantity, l.Total))))
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

            var paymentRequest = request.ToPaymentRequest(transactionEntity.Id);
            var payment = await paymentService.CreateAsync(paymentRequest);

            if (payment is not null)
            {
                var totalPaid = payment.Allocations
                    .Where(a => a.TransactionId == transactionEntity.Id)
                    .Sum(a => a.Amount);

                transactionEntity.AddPayment(totalPaid);
            }

            await context.SaveChangesAsync();
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
        var partnerBalance = await context.PartnerBalances
            .FirstAsync(x => x.PartnerId == request.PartnerId);
        var totalDebt = request.Type == Contracts.Enums.TransactionType.Sale
            ? Math.Abs(partnerBalance.PayableDebt)
            : Math.Abs(partnerBalance.ReceivableDebt);

        var totalDue = request.Lines.Sum(CalculateLineTotal);
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

    private static decimal CalculateLineTotal(CreateTransactionLine l)
        => l.UnitPrice * l.Quantity * (1 - (l.Discount / 100m));

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
