using Ombor.Contracts.Common;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class ProductMappings
{
    public static ProductDto ToDto(this Product product)
    {
        if (product.Category is null)
        {
            throw new InvalidOperationException("Cannot map product without Category.");
        }

        var images = product.Images
            .Select(x => x.ToDto())
            .ToArray();

        var totalStock = product.TotalStock();

        return new(
            Id: product.Id,
            CategoryId: product.CategoryId,
            CategoryName: product.Category.Name,
            Name: product.Name,
            SKU: product.SKU,
            Description: product.Description,
            Barcode: product.Barcode,
            SalePrice: product.SalePrice,
            SupplyPrice: product.SupplyPrice,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: totalStock <= product.LowStockThreshold,
            Measurement: product.Measurement.ToString(),
            Type: product.Type.ToString(),
            IsArchived: product.IsArchived,
            Images: images,
            WarehouseItems: product.WarehouseItemDtos(),
            TotalStock: totalStock,
            AverageCost: product.WeightedAverageCost(),
            Packaging: product.Packaging.ToDto());
    }

    public static Product ToEntity(this CreateProductRequest request)
    {
        return new()
        {
            Name = request.Name,
            SKU = request.SKU,
            Description = request.Description,
            Barcode = request.Barcode,
            SalePrice = request.SalePrice,
            SupplyPrice = request.SupplyPrice,
            LowStockThreshold = request.LowStockThreshold,
            Measurement = Enum.Parse<Domain.Enums.UnitOfMeasurement>(request.Measurement.ToString()),
            Type = Enum.Parse<Domain.Enums.ProductType>(request.Type.ToString()),
            Packaging = (request.Packaging ?? new(0, null, null)).ToEntity(), // TODO: Remove default value when upgraded to .NET 10
            CategoryId = request.CategoryId,
            Category = null!, // should be taken from CategoryId
            // Created at zero stock — stock arrives via opening-stock or supply (rule 17).
        };
    }

    public static CreateProductResponse ToCreateResponse(this Product product)
    {
        if (product.Category is null)
        {
            throw new InvalidOperationException("Cannot map product without Category.");
        }

        var totalStock = product.TotalStock();

        return new(
            Id: product.Id,
            CategoryId: product.CategoryId,
            CategoryName: product.Category.Name,
            Name: product.Name,
            SKU: product.SKU,
            Description: product.Description,
            Barcode: product.Barcode,
            SalePrice: product.SalePrice,
            SupplyPrice: product.SupplyPrice,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: totalStock <= product.LowStockThreshold,
            Measurement: product.Measurement.ToString(),
            Type: product.Type.ToString(),
            IsArchived: product.IsArchived,
            Images: product.Images.ToDto(),
            WarehouseItems: product.WarehouseItemDtos(),
            TotalStock: totalStock,
            AverageCost: product.WeightedAverageCost(),
            Packaging: product.Packaging.ToDto());
    }

    public static UpdateProductResponse ToUpdateResponse(this Product product)
    {
        if (product.Category is null)
        {
            throw new InvalidOperationException("Cannot map product without Category.");
        }

        return new(
            Id: product.Id,
            CategoryId: product.CategoryId,
            CategoryName: product.Category.Name,
            Name: product.Name,
            SKU: product.SKU,
            Description: product.Description,
            Barcode: product.Barcode,
            SalePrice: product.SalePrice,
            SupplyPrice: product.SupplyPrice,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: product.TotalStock() <= product.LowStockThreshold,
            Measurement: product.Measurement.ToString(),
            Type: product.Type.ToString(),
            IsArchived: product.IsArchived,
            Packaging: product.Packaging.ToDto());
    }

    public static void ApplyUpdate(this Product product, UpdateProductRequest request)
    {
        product.Name = request.Name;
        product.SKU = request.SKU;
        product.Description = request.Description;
        product.Barcode = request.Barcode;
        product.SalePrice = request.SalePrice;
        product.SupplyPrice = request.SupplyPrice;
        product.LowStockThreshold = request.LowStockThreshold;
        product.Measurement = Enum.Parse<Domain.Enums.UnitOfMeasurement>(request.Measurement.ToString());
        product.Type = Enum.Parse<Domain.Enums.ProductType>(request.Type.ToString());
        product.CategoryId = request.CategoryId;
        product.Packaging = (request.Packaging ?? new(0, null, null)).ToEntity(); // TODO: Remove default value when upgraded to .NET 10
    }

    public static Domain.Enums.ProductType ToDomain(this Contracts.Enums.ProductType type)
        => Enum.Parse<Domain.Enums.ProductType>(type.ToString());

    // Stock is the sum of per-warehouse warehouse items — WarehouseItem is the sole source (rule 17).
    private static decimal TotalStock(this Product product)
        => product.WarehouseItems.Sum(i => i.Quantity);

    // Value-weighted average cost across warehouses; null when there is no stock.
    private static decimal? WeightedAverageCost(this Product product)
    {
        var total = product.WarehouseItems.Sum(i => i.Quantity);

        return total == 0
            ? null
            : product.WarehouseItems.Sum(i => i.Quantity * i.AverageCost) / total;
    }

    private static ProductWarehouseItemDto[] WarehouseItemDtos(this Product product)
        => [.. product.WarehouseItems.Select(i => new ProductWarehouseItemDto(
            i.WarehouseId,
            i.Warehouse.Name,
            i.Quantity,
            i.AverageCost))];

    private static ProductPackagingDto? ToDto(this ProductPackaging packaging)
        => packaging.Size == 0
        ? null
        : new(
            Size: packaging.Size,
            Label: packaging.Label,
            Barcode: packaging.Barcode);

    private static ProductPackaging ToEntity(this ProductPackagingDto packaging)
        => new()
        {
            Size = packaging.Size,
            Label = packaging.Label,
            Barcode = packaging.Barcode
        };
}
