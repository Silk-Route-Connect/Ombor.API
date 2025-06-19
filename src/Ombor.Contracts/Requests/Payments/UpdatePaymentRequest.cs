using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payments;

public sealed record UpdatePaymentRequest(
    int PaymentId,
    int PartnerId,
    string? Notes,
    decimal Amount,
    decimal ExchangeRate,
    PaymentType Type,
    PaymentCurrency Currency,
    PaymentMethod Method,
    PaymentDirection Direction,
    IFormFile[]? AttachmentsToAdd,
    int[]? AttachmentsToDelete);
