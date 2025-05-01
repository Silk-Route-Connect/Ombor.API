using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Common.Factories;

public static class ProductRequestFactory
{
    private const int DefaultProductId = 10;
    private const int DefaultCategoryId = 10;

    public static CreateProductRequest GenerateValidCreateRequest(int? categoryId = null)
        => new(
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "Test Product Name",
            SKU: "Product SKU",
            Measurement: nameof(UnitOfMeasurement.Kilogram),
            Description: "Product Description",
            Barcode: "Product Barcode",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));

    public static CreateProductRequest GenerateInvalidCreateRequest(int? categoryId = null)
        => new(
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "", // Invalid Name
            SKU: "", // Invalid SKU
            Measurement: nameof(UnitOfMeasurement.Kilogram),
            Description: "Product Description",
            Barcode: "Product Barcode",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));

    public static UpdateProductRequest GenerateValidUpdateRequest(int? productId, int? categoryId = null)
        => new(
            Id: productId ?? DefaultProductId,
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "Product Name Before Update",
            SKU: "Product SKU Before Update",
            Measurement: nameof(UnitOfMeasurement.Kilogram),
            Description: "Product Description Before Update",
            Barcode: "Product Barcode Before Update",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));

    public static UpdateProductRequest GenerateInvalidUpdateRequest(int? productId, int? categoryId = null)
        => new(
            Id: productId ?? DefaultProductId,
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "", // Invalid Name
            SKU: "", // Invalid SKU
            Measurement: nameof(UnitOfMeasurement.Kilogram),
            Description: "Product Description",
            Barcode: "Product Barcode",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));
}
