namespace Ombor.TestDataGenerator.Helpers;

internal static class ProductHelpers
{
    private static readonly Random _rng = new();

    public static string GenerateSku(string productName, int categoryId)
    {
        var prefix = string.Empty;

        // if name has more than 2 words, take first letters from each, otherwise take first 3 letters
        if (productName.Split(' ').Length > 2)
        {
            prefix = string.Concat(
                productName
                    .Split(' ')
                    .Select(x => x[0])
                    .Take(3)
                    .Select(char.ToUpper)
            );
        }
        else
        {
            prefix = string.Concat(productName
                .Where(char.IsLetter)
                .Take(3)
                .Select(char.ToUpper)
                .ToArray());
        }

        if (prefix.Length < 3)
        {
            prefix = prefix.PadRight(3, 'X');   // pad if name is too short
        }

        var randomPart = _rng.Next(0, 10_000)
                             .ToString("D4");

        return $"{prefix}-{categoryId}-{randomPart}";
    }
}
