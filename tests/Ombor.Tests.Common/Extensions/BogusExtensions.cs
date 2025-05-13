using Bogus.DataSets;

namespace Ombor.Tests.Common.Extensions;

internal static class BogusExtensions
{
    public static string CategoryName(this Commerce commerce)
        => commerce.Categories(1)[0];
}
