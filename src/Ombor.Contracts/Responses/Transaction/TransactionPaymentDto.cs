namespace Ombor.Contracts.Responses.Transaction;

public sealed record TransactionPaymentDto(
    int Id,
    DateTimeOffset Date,
    decimal Amount,
    string Notes);
