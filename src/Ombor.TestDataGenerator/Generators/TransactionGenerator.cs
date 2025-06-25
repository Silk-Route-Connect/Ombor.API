using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.TestDataGenerator.Generators;

public static class TransactionGenerator
{
    private const string DefaultLocale = "en";
    private const int DefaultMaxItemsCount = 20;

    public static List<TransactionRecord> Generate(
        int partnerId,
        Product[] products,
        TransactionType type,
        bool fullyPaid = true,
        int maxItemsCount = DefaultMaxItemsCount,
        int count = 5,
        string locale = DefaultLocale)
        => GetGenerator(partnerId, products, type, fullyPaid, maxItemsCount, locale).Generate(count);

    private static Faker<TransactionRecord> GetGenerator(
        int partnerId,
        Product[] products,
        TransactionType type,
        bool fullyPaid = true,
        int maxItemsCount = DefaultMaxItemsCount,
        string locale = DefaultLocale)
        => new Faker<TransactionRecord>(locale)
        .RuleFor(x => x.Notes, f => f.Lorem.Sentence())
        .RuleFor(x => x.TransactionNumber, f => f.Finance.RoutingNumber())
        .RuleFor(x => x.Type, type)
        .RuleFor(x => x.Lines, (_, t) => GetTransactionLine(products, t.Type).GenerateBetween(5, maxItemsCount))
        .RuleFor(x => x.TotalDue, (_, t) => t.Lines.Sum(x => x.LineTotal))
        .RuleFor(x => x.TotalPaid, (f, t) => fullyPaid ? t.TotalDue : f.Random.Decimal(Math.Min(t.TotalDue * 0.3m, t.TotalDue * 0.8m)))
        .RuleFor(x => x.Status, (_, t) => t.TotalDue <= t.TotalPaid ? TransactionStatus.Closed : TransactionStatus.Open)
        .RuleFor(x => x.DateUtc, f => f.Date.BetweenOffset(f.Date.PastOffset(), f.Date.SoonOffset()))
        .RuleFor(x => x.PartnerId, partnerId);

    private static Faker<TransactionLine> GetTransactionLine(Product[] products, TransactionType type) => new Faker<TransactionLine>()
        .RuleFor(x => x.ProductId, f => f.PickRandom(products).Id)
        .RuleFor(x => x.UnitPrice, f => GetProductPrice(f.PickRandom(products), type))
        .RuleFor(x => x.Quantity, f => f.Random.Number(1, 20))
        .RuleFor(x => x.Discount, f => f.Random.Number(0, 0)); // Add logic to properly calculate discount

    private static decimal GetProductPrice(Product product, TransactionType type)
        => type switch
        {
            TransactionType.Sale or TransactionType.SaleRefund => product.SalePrice,
            TransactionType.Supply or TransactionType.SupplyRefund => product.SupplyPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
}
