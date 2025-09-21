namespace Ombor.Contracts.Requests.Common;
public sealed record PagedRequest(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortByDescending = false
    );