using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

/// <summary>
/// Every rule a <see cref="CreateTransactionRequest"/> must pass before anything moves: request validation, ownership
/// of each referenced id (rule 34), partner/type compatibility, the payment preconditions, settlement direction and
/// caps, and the refund rules (2–6). Split from <see cref="TransactionService"/>, which keeps the orchestration.
/// </summary>
internal sealed class TransactionCreateGuard(IApplicationDbContext context, IRequestValidator validator)
{
    /// <summary>Validates the request; returns the original (with its lines) when the request is a refund.</summary>
    public async Task<TransactionRecord?> ValidateAsync(CreateTransactionRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        if (!request.WarehouseId.HasValue)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.WarehouseId), "A warehouse (WarehouseId) is required for the transaction.")]);
        }

        await OwnedReferences.Check()
            .Require(context.Partners, request.PartnerId, nameof(request.PartnerId))
            .Require(context.Warehouses, request.WarehouseId, nameof(request.WarehouseId))
            .Require(context.Wallets, request.PaidAmount > 0m ? request.WalletId : null, nameof(request.WalletId))
            .Require(context.Transactions, request.OriginalTransactionId, nameof(request.OriginalTransactionId))
            .Require(context.Products, request.Lines.Select((l, i) => (l.ProductId, $"Lines[{i}].ProductId")))
            .Require(context.Transactions, (request.Settlements ?? []).Select((s, i) => (s.TransactionId, $"Settlements[{i}].TransactionId")))
            .ThrowIfMissingAsync();

        var original = await ValidateRefundAsync(request);

        var partner = await context.Partners.FirstAsync(x => x.Id == request.PartnerId);

        if (!partner.CanHandleTransaction(request.Type))
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.PartnerId), $"Partner of type: {partner.Type} cannot have transactions of type: {request.Type}")]);
        }

        if (request.PaidAmount > 0m && !request.WalletId.HasValue)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.WalletId), "A wallet is required when a payment is made.")]);
        }

        await ValidateSettlementsAsync(request);

        return original;
    }

    /// <summary>
    /// A refund is priced from its original, never from the request (backend-13): each line takes the original line's
    /// unit price and discount type, with a fixed discount pro-rated to the refunded quantity. When the original sold
    /// the product on several lines, the refund uses their quantity-weighted net unit price instead.
    /// </summary>
    public static void ApplyOriginalPricing(TransactionRecord refund, TransactionRecord original)
    {
        foreach (var line in refund.Lines)
        {
            var originalLines = original.Lines.Where(l => l.ProductId == line.ProductId).ToArray();

            if (originalLines.Length == 1)
            {
                var source = originalLines[0];
                line.UnitPrice = source.UnitPrice;
                line.DiscountType = source.DiscountType;
                line.Discount = source.DiscountType == DiscountType.Percentage || source.Quantity == 0m
                    ? source.Discount
                    : Math.Round(source.Discount * line.Quantity / source.Quantity, 2, MidpointRounding.AwayFromZero);
                continue;
            }

            var originalQuantity = originalLines.Sum(l => l.Quantity);
            line.UnitPrice = originalQuantity == 0m
                ? 0m
                : Math.Round(originalLines.Sum(l => l.Total) / originalQuantity, 2, MidpointRounding.AwayFromZero);
            line.Discount = 0m;
            line.DiscountType = DiscountType.Fixed;
        }

        refund.TotalDue = refund.Lines.Sum(l => l.Total);
    }

    private async Task ValidateSettlementsAsync(CreateTransactionRequest request)
    {
        var settlements = request.Settlements ?? [];

        if (settlements.Sum(s => s.Amount) > request.PaidAmount)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.Settlements), "Settlements cannot exceed the paid amount.")]);
        }

        if (settlements.Length == 0)
        {
            return;
        }

        var settledIds = settlements.Select(s => s.TransactionId).ToArray();
        var settledTransactions = await context.Transactions
            .Where(t => settledIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);
        var direction = request.Type.GetPaymentDirection();

        // Collapse duplicate rows per transaction so a repeated TransactionId can't overpay (or 500 on apply).
        var settlementsByTransaction = settlements
            .Select((s, index) => (s.TransactionId, s.Amount, Index: index))
            .GroupBy(s => s.TransactionId)
            .Select(g => (TransactionId: g.Key, Amount: g.Sum(s => s.Amount), Property: $"Settlements[{g.First().Index}].TransactionId"));

        foreach (var (transactionId, amount, property) in settlementsByTransaction)
        {
            var settled = settledTransactions[transactionId];

            if (settled.PartnerId != request.PartnerId)
            {
                throw new ValidationException(
                    [new ValidationFailure(property, $"Transaction {transactionId} does not belong to partner {request.PartnerId}.")]);
            }

            settled.EnsureSettlableBy(direction, property);

            if (amount > settled.UnpaidAmount)
            {
                throw new ValidationException(
                    [new ValidationFailure(property, $"Settlement of {amount} exceeds the remaining {settled.UnpaidAmount} on transaction {settled.Id}.")]);
            }
        }
    }

    /// <summary>Validates refund-specific rules (rules.md #2-6); returns the original for a refund, else null.</summary>
    private async Task<TransactionRecord?> ValidateRefundAsync(CreateTransactionRequest request)
    {
        var domainType = request.Type.ToDomainType();
        var isRefund = domainType is TransactionType.SaleRefund or TransactionType.SupplyRefund;

        if (!isRefund)
        {
            if (request.OriginalTransactionId.HasValue)
            {
                throw new ValidationException(
                    [new ValidationFailure(nameof(request.OriginalTransactionId), "OriginalTransactionId is only valid for refund transactions.")]);
            }

            return null;
        }

        if (!request.OriginalTransactionId.HasValue)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.OriginalTransactionId), "Refund transactions require an OriginalTransactionId.")]);
        }

        var original = await context.Transactions
            .Include(t => t.Lines)
            .AsNoTracking()
            .FirstAsync(t => t.Id == request.OriginalTransactionId.Value);

        // Rule 4: a refund cannot itself be refunded.
        if (original.Type is TransactionType.SaleRefund or TransactionType.SupplyRefund)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.OriginalTransactionId), "A refund transaction cannot itself be refunded.")]);
        }

        // Rule 3: refund type must match the original type.
        var expectedOriginalType = domainType == TransactionType.SaleRefund
            ? TransactionType.Sale
            : TransactionType.Supply;

        if (original.Type != expectedOriginalType)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.OriginalTransactionId), $"{domainType} must reference a {expectedOriginalType} transaction.")]);
        }

        // Goods go back to the partner who bought or supplied them; a refund booked to someone else would create a
        // payable/receivable for goods that partner never dealt in.
        if (original.PartnerId != request.PartnerId)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.PartnerId), $"A refund must be booked to the original transaction's partner ({original.PartnerId}).")]);
        }

        await ValidateRefundQuantitiesAsync(request, original);

        return original;
    }

    /// <summary>Rules 5-6: the total refunded quantity per product must not exceed the original quantity.</summary>
    private async Task ValidateRefundQuantitiesAsync(CreateTransactionRequest request, TransactionRecord original)
    {
        var originalQuantities = original.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var alreadyRefunded = await context.Transactions
            .Where(t => t.OriginalTransactionId == original.Id)
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

        // A package-entry line refunds pack count × package size and its request Quantity is ignored, so the cap
        // must count the same resolved base quantity the stock move will book (rule 21).
        var packageSizes = await context.LoadPackageSizesAsync(
            request.Lines.Where(l => l.PackageQuantity is > 0).Select(l => l.ProductId));

        // Aggregate this request's lines by product before the cap check. Two lines of the same product in
        // one request would otherwise each pass against the same persisted baseline while their sum exceeds
        // the original quantity (over-refund + over-restock).
        var requestedQuantities = request.Lines
            .Select((l, index) => (
                l.ProductId,
                Quantity: PackageEntry.Resolve(l.ProductId, l.Quantity, l.PackageQuantity, packageSizes).Quantity,
                Index: index))
            .GroupBy(l => l.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(l => l.Quantity), FirstIndex: g.First().Index));

        foreach (var (productId, requestedQuantity, index) in requestedQuantities)
        {
            if (!originalQuantities.TryGetValue(productId, out var originalQuantity))
            {
                throw new ValidationException(
                    [new ValidationFailure($"Lines[{index}].ProductId", $"Product {productId} is not part of the original transaction.")]);
            }

            alreadyRefunded.TryGetValue(productId, out var refundedQuantity);

            if (refundedQuantity + requestedQuantity > originalQuantity)
            {
                throw new ValidationException(
                    [new ValidationFailure($"Lines[{index}].Quantity", $"Refund quantity for product {productId} exceeds the original transaction quantity.")]);
            }
        }
    }
}
