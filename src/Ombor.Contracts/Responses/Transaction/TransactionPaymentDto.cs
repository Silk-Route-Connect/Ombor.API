namespace Ombor.Contracts.Responses.Transaction;

/// <summary>
/// Payment that settled part of the transaction balance.
/// Only header fields are returned; attachments and allocations
/// are available from the payment endpoint.
/// </summary>
public sealed record TransactionPaymentDto(
    int Id,
    int PaymentId,
    DateTimeOffset Date,
    decimal Amount,
    string? Notes);
