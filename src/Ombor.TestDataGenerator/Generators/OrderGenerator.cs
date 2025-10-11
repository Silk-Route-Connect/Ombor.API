using Bogus;
using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Generators;

internal static class OrderGenerator
{
    private static Random random = new Random();

    public static List<Order> Generate(int customerId, Product[] products, int maxCount)
        => GetGenerator(customerId, products).GenerateBetween(5, maxCount);

    private static Faker<Order> GetGenerator(int customerId, Product[] products) => new Faker<Order>()
        .RuleFor(x => x.CustomerId, customerId)
        .RuleFor(x => x.DateUtc, f => f.Date.BetweenOffset(f.Date.PastOffset(), f.Date.SoonOffset()))
        .RuleFor(x => x.Source, Domain.Enums.OrderSource.Telegram)
        .RuleFor(x => x.DeliveryAddress, _ => GetAddress())
        .RuleFor(x => x.Status, f => f.Random.Enum<Domain.Enums.OrderStatus>())
        .RuleFor(x => x.OrderNumber, f => f.Random.Guid().ToString("N").ToUpperInvariant()[..10])
        .RuleFor(x => x.Notes, f => f.Lorem.Sentence())
        .RuleFor(x => x.Lines, _ => GetLines(products));

    private static List<OrderLine> GetLines(Product[] products) => new Faker<OrderLine>()
        .RuleFor(x => x.Product, f => f.PickRandom(products))
        .RuleFor(x => x.Quantity, f => f.Random.Number(1, 10))
        .RuleFor(x => x.UnitPrice, (f, t) => t.Product.SalePrice)
        .RuleFor(x => x.Discount, (_, t) => GetDiscount(t.UnitPrice))
        .GenerateBetween(1, 5)
        .DistinctBy(x => x.Product)
        .ToList();

    private static Domain.Common.Address GetAddress() => new Faker<Domain.Common.Address>()
        .RuleFor(x => x.Latitude, f => (decimal)f.Address.Latitude())
        .RuleFor(x => x.Longtitude, f => (decimal)f.Address.Longitude())
        .Generate();

    private static decimal GetDiscount(decimal productPrice)
    {
        var roll = random.Next(0, 100);

        if (roll < 80)
        {
            return 0;
        }

        return random.Next(0, (int)productPrice);
    }
}
