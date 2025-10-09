using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Transaction;

public sealed class GetTransactionsRequest : PagedRequest
{
    public int? PartnerId { get; set; }
    public TransactionStatus? Status { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "date_desc";

    public GetTransactionsRequest() { }

    public GetTransactionsRequest(
        int? partnerId = null,
        TransactionStatus? status = null,
        TransactionType? type = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        string? sortBy = "date_desc",
        int pageNumber = 1,
        int pageSize = 10) : base(pageNumber, pageSize)
    {
        PartnerId = partnerId;
        Status = status;
        Type = type;
        FromDate = fromDate;
        ToDate = toDate;
        SearchTerm = searchTerm;
        SortBy = sortBy;
    }
}
