using Bogus;
using Ombor.TestDataGenerator.Builders.Entity;
using Ombor.TestDataGenerator.Interfaces.Builders;
using Ombor.TestDataGenerator.Interfaces.Builders.Entity;

namespace Ombor.TestDataGenerator.Builders;

public sealed class TestDataBuilder : ITestDataBuilder
{
    private static readonly Faker _faker = new();

    private ICategoryBuilder? _category;
    public ICategoryBuilder CategoryBuilder => _category ??= new CategoryBuilder(_faker);

    private IProductBuilder? _product;
    public IProductBuilder ProductBuilder => _product ??= new ProductBuilder(_faker);
}
