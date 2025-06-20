using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Payment;

/// <summary>Response returned after successfully creating a payment.</summary>
public sealed record CreatePaymentResponse(
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
    PaymentDirection Direction,
    PaymentCurrency Currency,
    PaymentAttachmentDto[] Attachments,
    PaymentAllocationDto[] Allocations);
