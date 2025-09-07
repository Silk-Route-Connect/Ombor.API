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
        var packaging = product.Packaging.Size == 0 ? null : product.Packaging.ToDto();

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
            RetailPrice: product.RetailPrice,
            QuantityInStock: product.QuantityInStock,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold,
            Measurement: product.Measurement.ToString(),
            Type: product.Type.ToString(),
            Packaging: packaging,
            Images: images);
    }

    public static Product ToEntity(this CreateProductRequest request)
    {
        // TODO: Remove default value when upgraded to .NET 10
        request = request with
        {
            Packaging = request.Packaging ?? new(0, null, null)
        };

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
            Measurement = Enum.Parse<Domain.Enums.UnitOfMeasurement>(request.Measurement.ToString()),
            Type = Enum.Parse<Domain.Enums.ProductType>(request.Type.ToString()),
            Packaging = request.Packaging.ToEntity(),
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
            Description: product.Description,
            Barcode: product.Barcode,
            SalePrice: product.SalePrice,
            SupplyPrice: product.SupplyPrice,
            RetailPrice: product.RetailPrice,
            QuantityInStock: product.QuantityInStock,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold,
            Measurement: product.Measurement.ToString(),
            Type: product.Type.ToString(),
            Packaging: product.Packaging?.ToDto(),
            Images: product.Images.ToDto());
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
            RetailPrice: product.RetailPrice,
            QuantityInStock: product.QuantityInStock,
            LowStockThreshold: product.LowStockThreshold,
            IsLowStock: product.QuantityInStock <= product.LowStockThreshold,
            Measurement: product.Measurement.ToString(),
            Type: product.Type.ToString(),
            Packaging: product.Packaging?.ToDto());
    }

    public static void ApplyUpdate(this Product product, UpdateProductRequest request)
    {
        // TODO: Remove default value when upgraded to .NET 10
        request = request with
        {
            Packaging = request.Packaging ?? new(0, null, null)
        };

        product.Name = request.Name;
        product.SKU = request.SKU;
        product.Description = request.Description;
        product.Barcode = request.Barcode;
        product.SalePrice = request.SalePrice;
        product.SupplyPrice = request.SupplyPrice;
        product.RetailPrice = request.RetailPrice;
        product.QuantityInStock = request.QuantityInStock;
        product.LowStockThreshold = request.LowStockThreshold;
        product.Measurement = Enum.Parse<Domain.Enums.UnitOfMeasurement>(request.Measurement.ToString());
        product.Type = Enum.Parse<Domain.Enums.ProductType>(request.Type.ToString());
        product.CategoryId = request.CategoryId;
        product.Packaging = request.Packaging.ToEntity();
    }

    public static Domain.Enums.ProductType ToDomain(this Contracts.Enums.ProductType type)
        => Enum.Parse<Domain.Enums.ProductType>(type.ToString());

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
