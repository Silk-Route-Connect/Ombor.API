namespace Ombor.Contracts.Responses.Common;

public sealed record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
