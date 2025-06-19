using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Payment;

/// <summary>Response returned after updating a payment.</summary>
public sealed record UpdatePaymentResponse(
    int Id,
    int? PartnerId,
    string? Notes,
    string? ExternalReference,
    decimal Amount,
    decimal AmountLocal,
    decimal ExchangeRate,
    DateTimeOffset DateUtc,
    PaymentType Type,
    PaymentMethod Method,
    PaymentCurrency Currency,
    PaymentAttachmentDto[] Attachments,
    PaymentAllocationDto[] Allocations);
