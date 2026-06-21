using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Interfaces;

public interface IWarehouseBuilder
{
    IWarehouseBuilder WithId(int? id = null);
    IWarehouseBuilder WithName(string? name = null);
    IWarehouseBuilder WithLocation(string? location = null);
    IWarehouseBuilder WithIsArchived(bool? isArchived = null);
    IWarehouseBuilder WithWarehouseItems(IEnumerable<WarehouseItem>? warehouseItems = null);
    Warehouse Build();
    Warehouse BuildAndPopulate();
}
