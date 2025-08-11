namespace Ombor.Contracts.Responses.Payment;

public sealed record TransactionPaymentDto(
    int PaymentId,
    int TransactionId,
    decimal Amount,
    string Currency,
    string Method,
    string? Notes,
    DateTimeOffset Date);
