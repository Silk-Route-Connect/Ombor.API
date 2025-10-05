namespace Ombor.Contracts.Requests.Common;

public abstract record PagedRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    protected PagedRequest() { }

    protected PagedRequest(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => pageSize
        };
    }
}