using System.Security.Cryptography.X509Certificates;
using Bogus;
using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Generators;

public static class EmployeeGenerator
{
    private const string DefaultLocale = "en";

    public static Employee Generate(string locale = DefaultLocale) => new Faker<Employee>(locale)
        .RuleFor(x => x.FullName, f => f.Person.FullName)
        .RuleFor(x => x.Role, f => f.Name.JobTitle())
        .RuleFor(x => x.IsActive, f => f.Random.Bool());

    public static List<Employee> Generate(int count, string locale = DefaultLocale) => new Faker<Employee>(locale)
        .RuleFor(x => x.FullName, f => f.Person.FullName)
        .RuleFor(x => x.Role, f => f.Name.JobTitle())
        .RuleFor(x => x.IsActive, f => f.Random.Bool())
        .Generate(count);
}
