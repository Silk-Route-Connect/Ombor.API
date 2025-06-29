using Moq;
using Ombor.Application.Mappings;
using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.InventoryServiceTests;

public abstract class InventoryTestsBase : ServiceTestsBase
{
    protected readonly int InventoryId = 1_000;
    protected readonly Inventory[] _defaultInventories;
    private protected readonly InventoryService _service;
    protected readonly Mock<IInventoryMapping> _mockMapping;

    protected InventoryTestsBase()
    {
        _defaultInventories = GenerateRandomInventories();
        SetupInventories(_defaultInventories);

        _mockMapping = new Mock<IInventoryMapping>();
        _service = new InventoryService(
            _mockContext.Object,
            _mockMapping.Object,
            _mockValidator.Object);
    }

    protected Inventory[] GenerateRandomInventories(int count = 5)
        => Enumerable.Range(1, count)
        .Select(x => CreateInventory(x))
        .ToArray();

    protected Inventory CreateInventory(int? id = null)
    => _builder.InventoryBuilder
        .WithId(id ?? InventoryId)
        .BuildAndPopulate();
}
