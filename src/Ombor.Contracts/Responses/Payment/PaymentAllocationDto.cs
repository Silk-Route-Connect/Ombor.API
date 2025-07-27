namespace Ombor.Contracts.Responses.Payment;

public sealed record PaymentAllocationDto(
    int Id,
    int PaymentId,
    int? TransactionId,
    decimal Amount,
    string Type);
