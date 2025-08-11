using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payment;

public sealed record CreateTransactionPaymentRequest(
    int TransactionId,
    string? Notes,
    bool ShouldReturnChange,
    IFormFile[] Attachments,
    CreatePaymentRequest[] Payments,
    CreateDebtPaymentRequest[]? DebtPayments);

public sealed record CreatePaymentRequest(
    decimal Amount,
    decimal ExchangeRate,
    string Currency,
    PaymentMethod Method);

public sealed record CreateDebtPaymentRequest(
    int TransactionId,
    decimal Amount);