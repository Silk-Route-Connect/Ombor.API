using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Transactions;

public sealed record CreateRefundRequest(
    int TransactionId,
    decimal TotalPaid,
    string? Notes,
    decimal Amount,
    decimal ExchangeRate,
    PaymentCurrency Currency,
    PaymentMethod Method,
    IFormFile[]? Attachments,
    CreateTransactionLine[] Lines);
