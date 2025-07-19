using Bogus;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

public sealed class TestDataBuilder : ITestDataBuilder
{
    private static readonly Faker _faker = new();

    public ICategoryBuilder CategoryBuilder => new CategoryBuilder(_faker);

    public IProductBuilder ProductBuilder => new ProductBuilder(_faker);

    public IProductImageBuilder ProductImageBuilder => new ProductImageBuilder(_faker);

    public IPartnerBuilder PartnerBuilder => new PartnerBuilder(_faker);
    public IEmployeeBuilder EmployeeBuilder => new EmployeeBuilder(_faker);
}
