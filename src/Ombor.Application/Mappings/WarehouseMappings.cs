using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

public static class WarehouseMappings
{
    // isDeletable is referential state the entity alone can't know (it spans other tables), so the
    // caller computes it and passes it in — totals stay a pure projection of the warehouse's own items.
    public static WarehouseDto ToDto(this Warehouse warehouse, bool isDeletable)
    {
        var items = warehouse.WarehouseItems;

        return new(
            Id: warehouse.Id,
            Name: warehouse.Name,
            Location: warehouse.Location,
            ProductCount: items.Count,
            TotalUnits: items.Sum(i => i.Quantity),
            StockValue: items.Sum(i => i.Quantity * i.AverageCost),
            IsArchived: warehouse.IsArchived,
            IsDeletable: isDeletable);
    }

    public static WarehouseStockItemDto ToStockItemDto(this WarehouseItem item)
    {
        if (item.Product is null)
        {
            throw new InvalidOperationException("Cannot map WarehouseItem to WarehouseStockItemDto because Product is null.");
        }

        return new(
            ProductId: item.ProductId,
            ProductName: item.Product.Name,
            Sku: item.Product.SKU,
            CategoryName: item.Product.Category?.Name,
            Measurement: item.Product.Measurement.ToString(),
            Quantity: item.Quantity,
            AverageCost: item.AverageCost,
            Value: item.Quantity * item.AverageCost);
    }

    public static Warehouse ToEntity(this CreateWarehouseRequest request) =>
        new()
        {
            Name = request.Name,
            Location = request.Location,
        };

    public static void ApplyUpdate(this Warehouse warehouse, UpdateWarehouseRequest request)
    {
        warehouse.Name = request.Name;
        warehouse.Location = request.Location;
    }
}
