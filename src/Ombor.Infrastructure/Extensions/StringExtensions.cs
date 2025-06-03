namespace Ombor.Infrastructure.Extensions;

internal static class StringExtensions
{
    public static bool ContainsAny(this string str, char[] chars)
        => str.IndexOfAny(chars) >= 0;
}
