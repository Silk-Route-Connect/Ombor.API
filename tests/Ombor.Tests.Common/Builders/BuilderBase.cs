using Bogus;

namespace Ombor.Tests.Common.Builders;

internal abstract class BuilderBase(Faker faker)
{
    protected readonly Faker _faker = faker;
}
