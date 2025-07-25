using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Transaction;

public sealed record GetTransactionsRequest(
    string? SearchTerm,
    int? PartnerId,
    TransactionStatus[] Statuses,
    TransactionType? Type);
