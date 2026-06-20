namespace Ombor.Contracts.Responses.Payment;

/// <summary>An open transaction a payment can settle, with its remaining amount (oldest-first / FIFO).</summary>
public sealed record OutstandingTransactionDto(
    int Id,
    DateTimeOffset Date,
    string Type,
    decimal Total,
    decimal Paid,
    decimal Remaining);
