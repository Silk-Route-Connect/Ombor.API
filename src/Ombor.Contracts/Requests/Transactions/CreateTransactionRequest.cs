using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Transactions;

/// <summary>
/// Multipart/form-data request that creates a new sale or supply
/// together with an optional up-front payment and file attachments.
/// </summary>
public sealed record CreateTransactionRequest(
    int PartnerId,
    TransactionType Type,
    string? Notes,
    decimal TotalPaid,
    decimal ExchangeRate,
    PaymentCurrency Currency,
    PaymentMethod PaymentMethod,
    IFormFile[]? Attachments,
    CreateTransactionLine[] Lines);

/// <summary>Line item supplied when a transaction is created.</summary>
public sealed record CreateTransactionLine(
    int ProductId,
    decimal UnitPrice,
    decimal Quantity,
    decimal Discount);
