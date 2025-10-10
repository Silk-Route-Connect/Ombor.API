using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Payment;

public sealed class GetPaymentsRequest : PagedRequest
{
    public int? PartnerId { get; set; }
    public int? TransactionId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public PaymentType? Type { get; set; }
    public PaymentDirection? Direction { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "date_desc";

    public GetPaymentsRequest() { }

    public GetPaymentsRequest(
        int? partnerId = null,
        int? transactionId = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        PaymentType? type = null,
        PaymentDirection? direction = null,
        string? searchTerm = null,
        string? sortBy = "date_desc",
        int pageNumber = 1,
        int pageSize = 10) : base(pageNumber, pageSize)
    {
        PartnerId = partnerId;
        TransactionId = transactionId;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        FromDate = fromDate;
        ToDate = toDate;
        Type = type;
        Direction = direction;
        SearchTerm = searchTerm;
        SortBy = sortBy;
    }
}
