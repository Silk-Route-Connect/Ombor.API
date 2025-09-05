using Bogus;
using Bogus.DataSets;

namespace Ombor.TestDataGenerator.Extensions;

public static class BogusExtensions
{
    public static string CategoryName(this Commerce commerce)
        => commerce.Categories(1)[0];

    public static decimal GetProductPrice(this Randomizer random, decimal min, decimal max)
    {
        var price = (int)random.Decimal(min, max);

        return price * 1_000;
    }

    public static decimal NextThousand(this Randomizer random, int minThousands, int maxThousands)
    {
        if (minThousands < 0 || maxThousands < 0) throw new ArgumentOutOfRangeException(nameof(minThousands));
        if (minThousands > maxThousands) (minThousands, maxThousands) = (maxThousands, minThousands);

        var k = random.Int(minThousands, maxThousands);
        return k * 1000m;
    }

    public static decimal Discount(this Randomizer random, int min, int max, int zeroProbabilityPercent)
    {
        if (min > max)
        {
            throw new ArgumentException("Min cannot be greater than max.");
        }

        if (zeroProbabilityPercent < 0 || zeroProbabilityPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(zeroProbabilityPercent), "Probability must be between 0 and 100.");
        }

        int roll = random.Number(0, 100);

        if (roll < zeroProbabilityPercent)
        {
            return 0;
        }

        return random.Number(min, max + 1);
    }
}
