using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Mappings;

internal static class ProductMappings
{
    public static Product ToEntity(this CreateProductRequest request)
    {
        if (!Enum.TryParse<UnitOfMeasurement>(request.Measurement, out var measurement))
        {
            throw new ArgumentException($"Invalid measurement: {request.Measurement}");
        }

        return new Product
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
            ExpireDate = request.ExpireDate,
            CategoryId = request.CategoryId,
            Category = null! // should be taken from CategoryId
        };
    }

    public static Product ToEntity(this UpdateProductRequest request)
    {
        if (!Enum.TryParse<UnitOfMeasurement>(request.Measurement, out var measurement))
        {
            throw new ArgumentException($"Invalid measurement: {request.Measurement}");
        }

        return new Product
        {
            Id = request.Id,
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
            ExpireDate = request.ExpireDate,
            CategoryId = request.CategoryId,
            Category = null! // should be taken from CategoryId
        };
    }

    public static CreateProductResponse ToCreateResponse(this Product product)
    {
        var thresholdDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

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
            ExpireDate: product.ExpireDate,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold,
            IsExpirationClose: product.ExpireDate >= thresholdDate);
    }

    public static UpdateProductResponse ToUpdateResponse(this Product product)
    {
        var thresholdDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

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
            ExpireDate: product.ExpireDate,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold,
            IsExpirationClose: product.ExpireDate >= thresholdDate);
    }

    public static ProductDto ToDto(this Product product)
    {
        if (product.Category is null)
        {
            throw new InvalidOperationException("Cannot map product to DTO because category is null.");
        }

        var thresholdDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

        return new ProductDto(
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
            ExpireDate: product.ExpireDate,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold,
            IsExpirationClose: product.ExpireDate >= thresholdDate);
    }

    public static void Update(this Product product, UpdateProductRequest request)
    {
        if (!Enum.TryParse<UnitOfMeasurement>(request.Measurement, out var measurement))
        {
            throw new ArgumentException($"Invalid measurement: {request.Measurement}");
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
        product.ExpireDate = request.ExpireDate;
        product.CategoryId = request.CategoryId;
    }
}
