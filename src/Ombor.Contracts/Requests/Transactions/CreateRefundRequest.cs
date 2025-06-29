using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Transactions;

/// <summary>
/// Multipart/form-data request that creates a refund document
/// plus a payment that settles the refund.
/// </summary>
public sealed record CreateRefundRequest(
    int TransactionId,
    string? Notes,
    decimal TotalPaid,
    decimal ExchangeRate,
    PaymentCurrency Currency,
    PaymentMethod Method,
    IFormFile[]? Attachments,
    RefundTransactionLine[] Lines);

/// <summary>Line item supplied when a refund is created.</summary>
public sealed record RefundTransactionLine(
    int ProductId,
    decimal UnitPrice,
    decimal Quantity,
    decimal Discount);
