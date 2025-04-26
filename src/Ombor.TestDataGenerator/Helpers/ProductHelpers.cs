namespace Ombor.TestDataGenerator.Helpers;

internal static class ProductHelpers
{
    private static readonly Random _rng = new();

    public static string GenerateSku(string productName, int categoryId)
    {
        // Prefix: first 3 letters of name, letters only, uppercase
        var prefix = new string(
            productName
                .Where(char.IsLetter)
                .Take(3)
                .Select(char.ToUpper)
                .ToArray()
        );

        if (prefix.Length < 3)
            prefix = prefix.PadRight(3, 'X');   // pad if name is too short

        var randomPart = _rng.Next(0, 10_000)
                             .ToString("D4");

        return $"{prefix}-{categoryId}-{randomPart}";
    }
}
