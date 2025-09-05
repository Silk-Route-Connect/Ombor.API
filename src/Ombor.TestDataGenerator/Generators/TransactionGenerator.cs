using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Extensions;

namespace Ombor.TestDataGenerator.Generators;

internal static class TransactionGenerator
{
    public static TransactionRecord Generate(int partnerId, TransactionType type, Product[] products)
        => GetGenerator(partnerId, type, products).Generate();

    public static List<TransactionRecord> Generate(int partnerId, TransactionType type, Product[] products, int maxCount)
        => GetGenerator(partnerId, type, products).GenerateBetween(1, maxCount);

    private static Faker<TransactionRecord> GetGenerator(int partnerId, TransactionType type, Product[] products) => new Faker<TransactionRecord>()
        .RuleFor(x => x.PartnerId, _ => partnerId)
        .RuleFor(x => x.DateUtc, f => f.Date.BetweenOffset(f.Date.PastOffset(), f.Date.SoonOffset()))
        .RuleFor(x => x.Type, type)
        .RuleFor(x => x.Status, TransactionStatus.Open)
        .RuleFor(x => x.Lines, _ => GenerateLines(products, type))
        .RuleFor(x => x.TotalDue, (_, t) => CalculateTotalDue(t.Lines))
        .RuleFor(x => x.TotalPaid, 0);

    private static TransactionLine[] GenerateLines(Product[] products, TransactionType type) => new Faker<TransactionLine>()
        .RuleFor(x => x.ProductId, f => f.PickRandom(products).Id)
        .RuleFor(x => x.UnitPrice, _ => GetRandomProductPrice(products, type))
        .RuleFor(x => x.Quantity, f => f.Random.Number(1, 10))
        .RuleFor(x => x.Discount, f => f.Random.Discount(1, 80, 90))
        .GenerateBetween(1, 10)
        .ToArray();

    private static decimal GetRandomProductPrice(Product[] products, TransactionType type)
    {
        var filteredProducts = type switch
        {
            TransactionType.Supply => [.. products.Where(p => p.Type != ProductType.Sale)],
            TransactionType.Sale => [.. products.Where(p => p.Type != ProductType.Supply)],
            _ => products
        };
        var product = filteredProducts[new Random().Next(filteredProducts.Length - 1)];

        return type == TransactionType.Sale ? product.SalePrice : product.SupplyPrice;
    }

    private static decimal CalculateTotalDue(IEnumerable<TransactionLine> lines)
        => lines.Sum(line => line.Quantity * line.UnitPrice * (1 - (line.Discount / 100)));
}
