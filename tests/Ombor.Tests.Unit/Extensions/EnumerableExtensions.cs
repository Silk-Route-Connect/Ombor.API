namespace Ombor.Tests.Unit.Extensions;

internal static class EnumerableExtensions
{
    private static readonly Random _random = new();

    public static T PickRandom<T>(this IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        var count = enumerable.Count();

        if (count == 0)
        {
            throw new InvalidOperationException("Cannot pick random element when collection has 0 elements.");
        }

        var randomIndex = _random.Next(count);

        return enumerable.ElementAt(randomIndex);
    }
}