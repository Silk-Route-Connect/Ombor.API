using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.TestDataGenerator.Generators;

public static class EmployeeGenerator
{
    private const string DefaultLocale = "en";

    public static Employee Generate(string locale = DefaultLocale)
        => GetGenerator(locale).Generate();

    public static List<Employee> Generate(int count, string locale = DefaultLocale)
        => GetGenerator(locale).Generate(count);

    private static Faker<Employee> GetGenerator(string locale = DefaultLocale) => new Faker<Employee>(locale)
        .RuleFor(x => x.FullName, f => f.Person.FullName)
        .RuleFor(x => x.Salary, f => f.Random.Decimal(100, 10000))
        .RuleFor(x => x.Position, f => f.Name.JobTitle())
        .RuleFor(x => x.Status, f => f.PickRandom<EmployeeStatus>())
        .RuleFor(x => x.DateOfEmployment, f => f.Date.PastDateOnly())
        .RuleFor(x => x.ContactInfo, f => new Domain.Common.ContactInfo
        {
            PhoneNumbers = [f.Phone.PhoneNumber("+998-9#-###-##-##")],
            Email = f.Person.Email,
            Address = f.Address.StreetAddress(),
            TelegramAccount = f.Internet.UserName()
        });
}
