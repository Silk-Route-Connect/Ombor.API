using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Payment;

public sealed record GetPaymentsRequest(
    int? PartnerId = null,
    int? TransactionId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    PaymentType? Type = null,
    PaymentDirection? Direction = null,
    string? SearchTerm = null,
    string? SortBy = "date_desc") : PagedRequest
{
    public GetPaymentsRequest(
        int? partnerId,
        int? transactionId,
        decimal? minAmount,
        decimal? maxAmount,
        DateTime? fromDate,
        DateTime? toDate,
        PaymentType? type,
        PaymentDirection? direction,
        string? searchTerm,
        string? sortBy,
        int pageNumber,
        int pageSize) : this(partnerId, transactionId, minAmount, maxAmount, fromDate, toDate, type, direction, searchTerm, sortBy
        )
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
