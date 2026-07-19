using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;

namespace Ombor.Contracts.Requests.Transaction;

/// <summary>
/// Creates a transaction (Sale/Supply/SaleRefund/SupplyRefund) and, when money changes hands,
/// the payment that settles it. The amount is drawn from <see cref="WalletId"/> and applied to
/// this transaction first, then to any <see cref="Settlements"/>; <see cref="Overpayment"/>
/// decides what happens to anything left over.
/// </summary>
/// <param name="PartnerId">The partner the transaction is with (required).</param>
/// <param name="Type">Sale, Supply, SaleRefund or SupplyRefund. Direction is derived from this.</param>
/// <param name="Notes">Optional free-text note.</param>
/// <param name="Lines">The transaction lines (at least one).</param>
/// <param name="WalletId">The wallet the payment moves through. Required when <see cref="PaidAmount"/> is greater than zero.</param>
/// <param name="PaidAmount">Total amount paid now. Zero leaves the transaction fully on account.</param>
/// <param name="Settlements">Other open transactions of the same partner this payment also settles.</param>
/// <param name="Overpayment">What to do with any amount beyond the settled debt (change or advance).</param>
/// <param name="Attachments">Optional file attachments.</param>
/// <param name="WarehouseId">The warehouse the stock moves through (required for stock movement).</param>
/// <param name="OriginalTransactionId">The transaction being refunded (required for refund types).</param>
/// <param name="RefundReason">Why the refund was issued (required for refund types).</param>
/// <param name="DueDate">When the outstanding amount is due. Optional; blank means due on receipt (no overdue tracking).</param>
public sealed record CreateTransactionRequest(
    int PartnerId,
    TransactionType Type,
    string? Notes,
    CreateTransactionLine[] Lines,
    int? WalletId,
    decimal PaidAmount,
    SettlementInput[]? Settlements,
    OverpaymentHandling Overpayment,
    IFormFile[] Attachments,
    int? WarehouseId = null,
    int? OriginalTransactionId = null,
    string? RefundReason = null,
    DateOnly? DueDate = null);

/// <summary>A single line of a transaction.</summary>
/// <param name="ProductId">The product sold or supplied.</param>
/// <param name="UnitPrice">Price per unit.</param>
/// <param name="Discount">Discount value, interpreted per <paramref name="DiscountType"/> (rule 37).</param>
/// <param name="DiscountType">Whether <paramref name="Discount"/> is a percentage or a fixed amount.</param>
/// <param name="Quantity">Quantity in base units. Ignored for a package entry (see <paramref name="PackageQuantity"/>), where the server computes the base quantity instead.</param>
/// <param name="PackageQuantity">The number of packages entered, for a package-entry line; null for a base-unit line. The server reads the product's package size, computes the base <paramref name="Quantity"/>, and snapshots the size (rule 21). The package size is never supplied by the client.</param>
public sealed record CreateTransactionLine(
    int ProductId,
    decimal UnitPrice,
    decimal Discount,
    DiscountType DiscountType,
    decimal Quantity,
    int? PackageQuantity = null);
