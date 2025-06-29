namespace Ombor.Contracts.Requests.Payments;

public sealed record GetTransactionPaymentsRequest(
    int TransactionId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);
