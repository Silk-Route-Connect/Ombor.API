using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Payment;

public sealed record PaymentAllocationDto(
    int Id,
    int? TransactionId,
    decimal AppliedAmount,
    PaymentAllocationType Type);
