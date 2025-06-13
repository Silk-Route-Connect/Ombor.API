using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.TestDataGenerator.Generators;

public static class PartnerGenerator
{
    private const string DefaultLocale = "en";

    public static Partner Generate(string locale = DefaultLocale) =>
        GetGenerator(locale).Generate();

    public static List<Partner> Generate(int count = 5, string locale = DefaultLocale) =>
        GetGenerator(locale).Generate(count);

    private static Faker<Partner> GetGenerator(string locale) => new Faker<Partner>(locale)
        .RuleFor(x => x.Name, f => f.Person.FullName)
        .RuleFor(x => x.Address, f => f.Address.FullAddress())
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.CompanyName, f => f.Person.Company.Name)
        .RuleFor(x => x.Balance, f => f.Random.Decimal(0, 100_000_000))
        .RuleFor(x => x.Type, f => f.Random.Enum<PartnerType>())
        .RuleFor(x => x.PhoneNumbers, f => [.. Enumerable.Range(0, f.Random.Number(5)).Select(_ => f.Phone.PhoneNumber("+998-9#-###-##-##"))]);
}
