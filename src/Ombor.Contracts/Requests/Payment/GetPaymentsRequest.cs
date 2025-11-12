using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Contracts.Requests.Payment;

public sealed record GetPaymentsRequest(
    int? PartnerId = null,
    int? EmployeeId = null,
    int? TransactionId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    PaymentType? Type = null,
    PaymentDirection? Direction = null,
    string? SearchTerm = null,
    string? SortBy = "date_desc",
    int PageNumber = 1,
    int PageSize = 10) : PagedRequest(PageNumber, PageSize);

