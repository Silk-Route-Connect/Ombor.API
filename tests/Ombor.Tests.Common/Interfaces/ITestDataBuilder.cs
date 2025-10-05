namespace Ombor.Tests.Common.Interfaces;

public interface ITestDataBuilder
{
    ICategoryBuilder CategoryBuilder { get; }
    IProductBuilder ProductBuilder { get; }
    IPartnerBuilder PartnerBuilder { get; }
    IProductImageBuilder ProductImageBuilder { get; }
    IEmployeeBuilder EmployeeBuilder { get; }
    IInventoryBuilder InventoryBuilder { get; }
}
