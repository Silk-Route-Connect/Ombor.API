using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payments;

public sealed record CreatePaymentRequest(
    int PartnerId,
    string? Notes,
    decimal Amount,
    decimal ExchangeRate,
    PaymentType Type,
    PaymentCurrency Currency,
    PaymentMethod Method,
    PaymentDirection Direction,
    IFormFile[]? Attachments);
