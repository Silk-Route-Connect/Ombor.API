using Bogus;
using Ombor.TestDataGenerator.Builders.Entity;
using Ombor.TestDataGenerator.Interfaces.Builders;
using Ombor.TestDataGenerator.Interfaces.Builders.Entity;

namespace Ombor.TestDataGenerator.Builders;

public sealed class TestDataBuilder : ITestDataBuilder
{
    private static readonly Faker _faker = new();

    public ICategoryBuilder CategoryBuilder => new CategoryBuilder(_faker);

    public IProductBuilder ProductBuilder => new ProductBuilder(_faker);
}
