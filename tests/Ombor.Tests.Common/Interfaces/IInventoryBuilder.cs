using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Interfaces;

public interface IInventoryBuilder
{
    IInventoryBuilder WithId(int? id = null);
    IInventoryBuilder WithName(string? name = null);
    IInventoryBuilder WithLocation(string? location = null);
    IInventoryBuilder WithIsAction(bool? isActive = null);
    IInventoryBuilder WithInventoryItems(IEnumerable<InventoryItem>? inventoryItems = null);
    Inventory Build();
    Inventory BuildAndPopulate();
}
