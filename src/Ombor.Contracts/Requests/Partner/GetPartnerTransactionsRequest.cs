using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Partner;

public sealed record GetPartnerTransactionsRequest(
    int PartnerId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    TransactionType? Type);
