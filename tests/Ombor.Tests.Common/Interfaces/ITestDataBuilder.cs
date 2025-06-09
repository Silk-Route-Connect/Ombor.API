namespace Ombor.Tests.Common.Interfaces;

public interface ITestDataBuilder
{
    ICategoryBuilder CategoryBuilder { get; }
    IProductBuilder ProductBuilder { get; }
    ISupplierBuilder SupplierBuilder { get; }
    IProductImageBuilder ProductImageBuilder { get; }
}
