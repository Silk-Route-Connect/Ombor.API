using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Mappings;

internal static class ProductMappings
{
    public static ProductDto ToDto(this Product product)
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
            Measurement: product.Measurement.ToString(),
            Description: product.Description,
            Barcode: product.Barcode,
            SalePrice: product.SalePrice,
            SupplyPrice: product.SupplyPrice,
            RetailPrice: product.RetailPrice,
            QuantityInStock: product.QuantityInStock,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold);
    }

    public static Product ToEntity(this CreateProductRequest request)
    {
        if (!Enum.TryParse<UnitOfMeasurement>(request.Measurement, out var measurement))
        {
            measurement = UnitOfMeasurement.None;
        }

        return new()
        {
            Name = request.Name,
            SKU = request.SKU,
            Description = request.Description,
            Barcode = request.Barcode,
            SalePrice = request.SalePrice,
            SupplyPrice = request.SupplyPrice,
            RetailPrice = request.RetailPrice,
            QuantityInStock = request.QuantityInStock,
            LowStockThreshold = request.LowStockThreshold,
            Measurement = measurement,
            CategoryId = request.CategoryId,
            Category = null! // should be taken from CategoryId
        };
    }

    public static CreateProductResponse ToCreateResponse(this Product product)
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
            Measurement: product.Measurement.ToString(),
            Description: product.Description,
            Barcode: product.Barcode,
            SalePrice: product.SalePrice,
            SupplyPrice: product.SupplyPrice,
            RetailPrice: product.RetailPrice,
            QuantityInStock: product.QuantityInStock,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold);
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
            Measurement: product.Measurement.ToString(),
            Description: product.Description,
            Barcode: product.Barcode,
            SalePrice: product.SalePrice,
            SupplyPrice: product.SupplyPrice,
            RetailPrice: product.RetailPrice,
            QuantityInStock: product.QuantityInStock,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold);
    }

    public static void ApplyUpdate(this Product product, UpdateProductRequest request)
    {
        if (!Enum.TryParse<UnitOfMeasurement>(request.Measurement, out var measurement))
        {
            measurement = UnitOfMeasurement.None;
        }

        product.Name = request.Name;
        product.SKU = request.SKU;
        product.Description = request.Description;
        product.Barcode = request.Barcode;
        product.SalePrice = request.SalePrice;
        product.SupplyPrice = request.SupplyPrice;
        product.RetailPrice = request.RetailPrice;
        product.QuantityInStock = request.QuantityInStock;
        product.LowStockThreshold = request.LowStockThreshold;
        product.Measurement = measurement;
        product.CategoryId = request.CategoryId;
    }
}
