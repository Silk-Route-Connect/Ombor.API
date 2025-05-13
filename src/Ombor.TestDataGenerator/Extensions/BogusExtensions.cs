using Bogus.DataSets;

namespace Ombor.TestDataGenerator.Extensions;

public static class BogusExtensions
{
    public static string CategoryName(this Commerce commerce)
        => commerce.Categories(1)[0];
}
