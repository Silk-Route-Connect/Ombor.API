using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.TestDataGenerator.Generators;

public static class TemplateGenerator
{
    private const string DefaultLocale = "en";

    public static Template Generate(int[] productIds, string? locale = DefaultLocale)
        => GetGenerator(productIds, locale).Generate();

    public static List<Template> Generate(int[] productIds, int count = 5, string? locale = DefaultLocale)
        => GetGenerator(productIds, locale).Generate(count).ToList();

    private static Faker<Template> GetGenerator(int[] productIds, string? locale = DefaultLocale) => new Faker<Template>(locale)
        .RuleFor(x => x.Name, f => $"{f.Company.CompanyName()} {f.Person.FirstName}")
        .RuleFor(x => x.Type, f => f.Random.Enum<TemplateType>())
        .RuleFor(x => x.Items, _ => GetItems(productIds));

    private static List<TemplateItem> GetItems(int[] productIds) => new Faker<TemplateItem>()
        .RuleFor(x => x.Quantity, f => f.Random.Number(2, 10))
        .RuleFor(x => x.UnitPrice, f => f.Random.Decimal(10_000, 1_000_000))
        .RuleFor(x => x.DiscountAmount, (f, ti) => f.Random.Decimal(500, ti.UnitPrice - 5_000))
        .RuleFor(x => x.ProductId, f => f.PickRandom(productIds))
        .GenerateBetween(5, 10)
        .ToList();
}
