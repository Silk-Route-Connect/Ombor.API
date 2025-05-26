using Bogus;
using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Generators;

public static class CategoryGenerator
{
    private const string DefaultLocale = "en";

    public static Category Generate(string locale = DefaultLocale) => new Faker<Category>(locale)
        .RuleFor(x => x.Name, f => f.Commerce.Categories(1)[0])
        .RuleFor(x => x.Description, f => f.Lorem.Sentence());

    public static List<Category> Generate(int count, string locale = DefaultLocale) => new Faker<Category>(locale)
        .RuleFor(x => x.Name, f => f.Commerce.Categories(1)[0])
        .RuleFor(x => x.Description, f => f.Lorem.Sentence())
        .Generate(count);
}
