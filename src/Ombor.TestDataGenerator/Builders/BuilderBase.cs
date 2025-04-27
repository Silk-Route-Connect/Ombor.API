using Bogus;

namespace Ombor.TestDataGenerator.Builders;

internal abstract class BuilderBase(Faker faker)
{
    protected readonly Faker _faker = faker;
}
