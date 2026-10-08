using Ombor.Application.Extensions;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

public static class WarehouseMappings
{
    // isDeletable and lowStockCount span other tables (references; the products' archive state), so the caller
    // computes them and passes them in — the other totals stay a pure projection of the warehouse's own items. There is
    // no unit total: quantities of different products (kg, pieces, tonnes) are never added up (DR-40).
    public static WarehouseDto ToDto(this Warehouse warehouse, bool isDeletable, int lowStockCount)
    {
        var items = warehouse.WarehouseItems;

        return new(
            Id: warehouse.Id,
            Name: warehouse.Name,
            Location: warehouse.Location,
            ProductCount: items.Count(i => i.Quantity > 0m),
            LowStockCount: lowStockCount,
            StockValue: items.Sum(i => i.Quantity * i.AverageCost),
            IsArchived: warehouse.IsArchived,
            IsDeletable: isDeletable);
    }

    public static WarehouseStockItemDto ToStockItemDto(this WarehouseItem item, bool warehouseIsArchived)
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
            Value: item.Quantity * item.AverageCost,
            LowStockThreshold: item.LowStockThreshold,
            IsLowStock: LowStock.IsLowStock(item.Quantity, item.LowStockThreshold, item.Product.IsArchived, warehouseIsArchived));
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
