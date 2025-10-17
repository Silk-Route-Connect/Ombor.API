namespace Ombor.Contracts.Requests.Common;

public abstract record PagedRequest(int PageNumber = 1, int PageSize = 10);