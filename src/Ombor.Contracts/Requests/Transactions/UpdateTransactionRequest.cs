using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Transactions;

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

public sealed record UpdateTransactionLine(
    int Id,
    int ProductId,
    decimal UnitPrice,
    decimal Quantity,
    decimal Discount);
