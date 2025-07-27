using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;

namespace Ombor.Contracts.Requests.Transaction;

public sealed record CreateTransactionRequest(
    int PartnerId,
    TransactionType Type,
    string? Notes,
    CreateTransactionLine[] Lines,
    CreatePaymentRequest[] Payments,
    CreateDebtPaymentRequest[] DebtPayments,
    bool ShouldReturnChange,
    IFormFile[] Attachments);

public sealed record CreateTransactionLine(
    int ProductId,
    decimal UnitPrice,
    decimal Discount,
    int Quantity);
