using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payments;

public sealed record CreateTransactionPaymentRequest(
    int TransactionId,
    string? Notes,
    decimal Amount,
    decimal ExchangeRate,
    PaymentCurrency Currency,
    PaymentMethod Method,
    IFormFile[]? Attachments);
