using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Payment;

/// <summary>
/// One allocation slice inside a payment.
/// <c>TransactionId</c> is null when the slice represents an advance payment
/// that is still on the partner’s balance.
/// </summary>
public sealed record PaymentAllocationDto(
    int Id,
    int? TransactionId,
    decimal AppliedAmount,
    PaymentAllocationType Type);
