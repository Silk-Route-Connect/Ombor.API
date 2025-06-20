using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Transactions;

/// <summary>
/// Multipart/form-data request that updates a transaction.
/// The transaction identifier is taken from the route.
/// </summary>
public sealed record UpdateTransactionRequest(
    int PartnerId,
    decimal TotalPaid,
    string? Notes,
    decimal Amount,
    decimal ExchangeRate,
    TransactionType Type,
    PaymentCurrency Currency,
    PaymentMethod Method,
    IFormFile[]? AttachmentsToAdd,
    int[]? AttachmentsToDelete,
    UpdateTransactionLine[] Lines);

/// <summary>Line item supplied when a transaction is updated.</summary>
public sealed record UpdateTransactionLine(
    int Id,
    int ProductId,
    decimal UnitPrice,
    decimal Quantity,
    decimal Discount);
