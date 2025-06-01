namespace Ombor.Tests.Common.Interfaces;

public interface ITestDataBuilder
{
    ICategoryBuilder CategoryBuilder { get; }
    IProductBuilder ProductBuilder { get; }
    IProductImageBuilder ProductImageBuilder { get; }
}
