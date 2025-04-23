using AutoFixture;

namespace Ombor.Tests.Unit.Extensions;

internal static class AutoFixtureExtensions
{
    public static List<T> CreateList<T>(this IFixture fixture, int itemsCount = 5)
        => fixture.CreateMany<T>(itemsCount).ToList();

    public static T[] CreateArray<T>(this IFixture fixture, int itemsCount = 5)
        => fixture.CreateMany<T>(itemsCount).ToArray();

    public static Exception CreateException(this IFixture fixture)
        => fixture.Create<Exception>();
}
