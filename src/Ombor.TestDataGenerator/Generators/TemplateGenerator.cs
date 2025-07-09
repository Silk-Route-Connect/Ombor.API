using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.TestDataGenerator.Generators;

public static class TemplateGenerator
{
    private const string DefaultLocale = "en";
    private const int DefaultMaxItemsCount = 10;

    public static Template Generate(int partnerId, int[] productIds, int maxItemsCount = DefaultMaxItemsCount, string? locale = DefaultLocale)
        => GetGenerator(partnerId, productIds, maxItemsCount, locale).Generate();

    public static List<Template> Generate(int partnerId, int[] productIds, int count = 5, int maxItemsCount = DefaultMaxItemsCount, string? locale = DefaultLocale)
        => GetGenerator(partnerId, productIds, maxItemsCount, locale).Generate(count);

    private static Faker<Template> GetGenerator(int partnerId, int[] productIds, int maxItemsCount = DefaultMaxItemsCount, string? locale = DefaultLocale) => new Faker<Template>(locale)
        .RuleFor(x => x.PartnerId, partnerId)
        .RuleFor(x => x.Name, f => $"{f.Company.CompanyName()} {f.Person.FirstName}")
        .RuleFor(x => x.Type, f => f.Random.Enum<TemplateType>())
        .RuleFor(x => x.Items, _ => GetItems(productIds, maxItemsCount));

    private static List<TemplateItem> GetItems(int[] productIds, int maxItemsCount) => new Faker<TemplateItem>()
        .RuleFor(x => x.Quantity, f => f.Random.Number(2, 10))
        .RuleFor(x => x.UnitPrice, f => f.Random.Decimal(10_000, 1_000_000))
        .RuleFor(x => x.DiscountAmount, (f, ti) => f.Random.Decimal(500, ti.UnitPrice - 5_000))
        .RuleFor(x => x.ProductId, f => f.PickRandom(productIds))
        .GenerateBetween(1, maxItemsCount);
}
