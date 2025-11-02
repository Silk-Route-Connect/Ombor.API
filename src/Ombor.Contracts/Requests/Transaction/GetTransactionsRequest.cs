using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Transaction;

public sealed record GetTransactionsRequest(
    int? PartnerId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    TransactionType? Type = null,
    TransactionStatus? Status = null,
    string? SearchTerm = null,
    string? SortBy = "date_desc",
    int PageNumber = 1,
    int PageSize = 10) : PagedRequest(PageNumber, PageSize);
