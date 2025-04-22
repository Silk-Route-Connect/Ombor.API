using Bogus;
using Ombor.Domain.Entities;

namespace Ombor.TestDataGenerator.Generators.Entities;

public static class CategoryGenerator
{
    public static Category Generate() => new Faker<Category>("ru")
        .RuleFor(x => x.Name, f => f.Commerce.Categories(1)[0])
        .RuleFor(x => x.Description, f => f.Lorem.Text());

    public static IEnumerable<Category> Generate(int count) => new Faker<Category>("ru")
        .RuleFor(x => x.Name, f => f.Commerce.Categories(1)[0])
        .RuleFor(x => x.Description, f => f.Lorem.Text())
        .Generate(count);
}
