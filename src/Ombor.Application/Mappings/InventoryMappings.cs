using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

public static class InventoryMappings
{
    public static InventoryDto ToDto(this Inventory inventory)
    {
        var items = inventory.InventoryItems
            .Select(ToDto)
            .ToArray();

        return new(Id: inventory.Id,
            Name: inventory.Name,
            Location: inventory.Location,
            IsActive: inventory.IsActive,
            InventoryItems: items);
    }

    public static InventoryItemDto ToDto(this InventoryItem inventoryItem) =>
        new(
            Id: inventoryItem.Id,
            Quantity: inventoryItem.Quantity,
            ProductName: inventoryItem.Product.Name,
            InventoryId: inventoryItem.InventoryId,
            ProductId: inventoryItem.ProductId);

    public static Inventory ToEntity(this CreateInventoryRequest request) =>
        new()
        {
            Name = request.Name,
            Location = request.Location,
            IsActive = request.IsActive
        };

    public static CreateInventoryResponse ToCreateResponse(this Inventory inventory)
    {
        var items = inventory.InventoryItems
            .Select(ToDto)
            .ToArray();

        return new(Id: inventory.Id,
            Name: inventory.Name,
            Location: inventory.Location,
            IsActive: inventory.IsActive,
            InventoryItems: items);
    }

    public static UpdateInventoryResponse ToUpdateResponse(this Inventory inventory)
    {
        var items = inventory.InventoryItems
            .Select(ToDto)
            .ToArray();

        return new(Id: inventory.Id,
            Name: inventory.Name,
            Location: inventory.Location,
            IsActive: inventory.IsActive,
            InventoryItems: items);
    }

    public static void ApplyUpdate(this Inventory inventory, UpdateInventoryRequest request)
    {
        inventory.Name = request.Name;
        inventory.Location = request.Location;
        inventory.IsActive = request.IsActive;
    }
}
