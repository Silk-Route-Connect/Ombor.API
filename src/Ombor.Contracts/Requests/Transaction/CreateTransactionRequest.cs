using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;

namespace Ombor.Contracts.Requests.Transaction;

public sealed record CreateTransactionRequest(
    int PartnerId,
    TransactionType Type,
    CreateTransactionLine[] Lines,
    string? Notes,
    IFormFile[]? Attachments,
    CreatePaymentRequest[] Payments,
    CreateDebtPaymentRequest[]? DebtPayments);

public sealed record CreateTransactionLine(
    int ProductId,
    decimal UnitPrice,
    decimal Discount,
    decimal Quantity);
