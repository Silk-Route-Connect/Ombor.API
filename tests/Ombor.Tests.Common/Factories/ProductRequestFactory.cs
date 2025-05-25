using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Product;

namespace Ombor.Tests.Common.Factories;

public static class ProductRequestFactory
{
    private const int DefaultProductId = 10;
    private const int DefaultCategoryId = 5;

    public static CreateProductRequest GenerateValidCreateRequest(int? categoryId = null)
        => new(
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "Test Product Name",
            SKU: "Product SKU",
            Description: "Product Description",
            Barcode: "Product Barcode",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            Measurement: UnitOfMeasurement.Kilogram,
            Type: Contracts.Enums.ProductType.All);

    public static CreateProductRequest GenerateInvalidCreateRequest(int? categoryId = null)
        => new(
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "", // Invalid Name
            SKU: "", // Invalid SKU
            Description: "Product Description",
            Barcode: "Product Barcode",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            Measurement: UnitOfMeasurement.Kilogram,
            Type: ProductType.All);

    public static UpdateProductRequest GenerateValidUpdateRequest(int? productId, int? categoryId = null)
        => new(
            Id: productId ?? DefaultProductId,
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "Updated Name",
            SKU: $"SKU - {Guid.NewGuid()}",
            Description: "Updated Description",
            Barcode: "Updated Barcode",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            Measurement: UnitOfMeasurement.Kilogram,
            Type: ProductType.Supply);

    public static UpdateProductRequest GenerateInvalidUpdateRequest(int? productId, int? categoryId = null)
        => new(
            Id: productId ?? DefaultProductId,
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "", // Invalid Name
            SKU: "", // Invalid SKU
            Description: "Product Description",
            Barcode: "Product Barcode",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            Measurement: UnitOfMeasurement.Kilogram,
            Type: ProductType.Sale);
}
