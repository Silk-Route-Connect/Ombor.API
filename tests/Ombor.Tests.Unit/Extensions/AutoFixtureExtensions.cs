using AutoFixture;

namespace Ombor.Tests.Unit.Extensions;

internal static class AutoFixtureExtensions
{
    /// <summary>
    /// Creates a list of elements.
    /// </summary>
    public static List<T> CreateList<T>(this IFixture fixture, int itemsCount = 5)
        => fixture.CreateMany<T>(itemsCount).ToList();

    /// <summary>
    /// Creates an array of elements.
    /// </summary>
    public static T[] CreateArray<T>(this IFixture fixture, int itemsCount = 5)
        => fixture.CreateMany<T>(itemsCount).ToArray();

    /// <summary>
    /// Returns random exception object.
    /// </summary>
    public static Exception CreateException(this IFixture fixture)
        => fixture.Create<Exception>();
}
