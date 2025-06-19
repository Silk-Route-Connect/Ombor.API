using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Transaction;

public sealed record UpdateTransactionResponse(
    int Id,
    int PartnerId,
    string PartnerName,
    string? Notes,
    string? TransactionNumber,
    decimal TotalDue,
    decimal TotalPaid,
    TransactionType Type,
    TransactionStatus Status,
    TransactionLineDto[] Lines);
