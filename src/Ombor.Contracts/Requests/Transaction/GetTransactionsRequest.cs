using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Transaction;

public sealed record GetTransactionsRequest(
    int? PartnerId = null,
    TransactionStatus? Status = null,
    TransactionType? Type = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    string? SortBy = "date_desc") : PagedRequest
{
    public GetTransactionsRequest(
        int? partnerId,
        TransactionStatus? status,
        TransactionType type,
        DateTime? fromDate,
        DateTime? toDate,
        string? searchTerm,
        string? sortBy,
        int pageNumber,
        int pageSize) : this(partnerId, status, type, fromDate, toDate, searchTerm, sortBy)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
