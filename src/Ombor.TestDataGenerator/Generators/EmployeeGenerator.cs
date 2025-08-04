using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.TestDataGenerator.Generators;

public static class EmployeeGenerator
{
    private const string DefaultLocale = "en";

    public static Employee Generate(string locale = DefaultLocale) => new Faker<Employee>(locale)
        .RuleFor(x => x.FullName, f => f.Person.FullName)
        .RuleFor(x => x.Salary, f => f.Random.Decimal(100, 10000))
        .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber("+998-9#-###-##-##"))
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.Address, f => f.Address.StreetAddress())
        .RuleFor(x => x.Description, f => f.Lorem.Sentences())
        .RuleFor(x => x.Role, f => f.PickRandom<EmployeeRole>())
        .RuleFor(x => x.Access, f => f.PickRandom<EmployeeAccess>())
        .RuleFor(x => x.Status, f => f.PickRandom<EmployeeStatus>())
        .RuleFor(x => x.DateOfEmployment, f => f.Date.PastDateOnly());

    public static List<Employee> Generate(int count, string locale = DefaultLocale) => new Faker<Employee>(locale)
        .RuleFor(x => x.FullName, f => f.Person.FullName)
        .RuleFor(x => x.Salary, f => f.Random.Decimal(100, 10000))
        .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber("+998-9#-###-##-##"))
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.Address, f => f.Address.StreetAddress())
        .RuleFor(x => x.Description, f => f.Lorem.Sentences())
        .RuleFor(x => x.Role, f => f.PickRandom<EmployeeRole>())
        .RuleFor(x => x.Access, f => f.PickRandom<EmployeeAccess>())
        .RuleFor(x => x.Status, f => f.PickRandom<EmployeeStatus>())
        .RuleFor(x => x.DateOfEmployment, f => f.Date.PastDateOnly())
        .Generate(count);
}
