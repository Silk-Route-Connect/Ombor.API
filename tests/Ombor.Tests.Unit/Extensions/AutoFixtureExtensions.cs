using AutoFixture;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Tests.Unit.Extensions;

internal static class AutoFixtureExtensions
{
    public static PagedList<T> CreatePagedList<T>(this IFixture fixture, int itemsCount = 5)
    {
        var items = fixture.CreateMany<T>(itemsCount).ToList();
        var totalCount = items.Count;
        const int pageNumber = 1;
        const int pageSize = 10;

        return new PagedList<T>(items, totalCount, pageNumber, pageSize);
    }

    public static PagedList<T> CreateEmptyPagedList<T>(this IFixture fixture)
      => new(Enumerable.Empty<T>(), 0, 1, 10);

    public static Exception CreateException(this IFixture fixture)
        => fixture.Create<Exception>();
}
