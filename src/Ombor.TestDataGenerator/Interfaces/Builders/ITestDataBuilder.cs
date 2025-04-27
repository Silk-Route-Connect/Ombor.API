using Ombor.TestDataGenerator.Interfaces.Builders.Entity;

namespace Ombor.TestDataGenerator.Interfaces.Builders;

public interface ITestDataBuilder
{
    ICategoryBuilder CategoryBuilder { get; }
    IProductBuilder ProductBuilder { get; }
}
