namespace Ombor.Contracts.Requests.Common;

public class PagedList<T> : List<T>
{
    public MetaData MetaData { get; set; }

    public PagedList() { }

    public PagedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
    {
        MetaData = new MetaData
        {
            TotalCount = count,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalPage = (int)Math.Ceiling(count / (double)pageSize)
        };

        AddRange(items);
    }

    public static PagedList<T> ToPagedList(IEnumerable<T> source, int count, int pageNumber, int pageSize)
    {
        var items = source.ToList();

        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}
