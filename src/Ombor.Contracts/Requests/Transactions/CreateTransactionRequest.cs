using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Transactions;

public sealed record CreateTransactionRequest(
    int PartnerId,
    TransactionType Type,
    decimal TotalPaid,
    string? Notes,
    decimal Amount,
    decimal ExchangeRate,
    PaymentCurrency Currency,
    PaymentMethod Method,
    IFormFile[]? Attachments,
    CreateTransactionLine[] Lines);

public sealed record CreateTransactionLine(
    int ProductId,
    decimal UnitPrice,
    decimal Quantity,
    decimal Discount);
