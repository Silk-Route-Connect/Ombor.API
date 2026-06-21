using Moq;
using Ombor.Application.Interfaces;
using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.WarehouseServiceTests;

public abstract class WarehouseTestsBase : ServiceTestsBase
{
    protected readonly int WarehouseId = 1_000;
    protected readonly Warehouse[] _defaultWarehouses;
    private protected readonly WarehouseService _service;

    protected WarehouseTestsBase()
    {
        _defaultWarehouses = GenerateRandomWarehouses();
        SetupWarehouses(_defaultWarehouses);

        _service = new WarehouseService(
            _mockContext.Object,
            _mockValidator.Object,
            Mock.Of<ICurrentUserAccessor>());
    }

    protected Warehouse[] GenerateRandomWarehouses(int count = 5)
        => Enumerable.Range(1, count)
        .Select(x => CreateWarehouse(x))
        .ToArray();

    protected Warehouse CreateWarehouse(int? id = null)
    => _builder.WarehouseBuilder
        .WithId(id ?? WarehouseId)
        .BuildAndPopulate();
}
