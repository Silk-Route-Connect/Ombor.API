using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Transactions;

public sealed record GetTransactionsRequest(
    string? SearchTerm,
    int? PartnerId,
    TransactionType? Type,
    TransactionStatus? Status);
