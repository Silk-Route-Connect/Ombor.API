using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;

namespace Ombor.Tests.Common.Factories;

public static class ProductRequestFactory
{
    private const string ImageFolder = "Resources/Images";
    private const int DefaultProductId = 10;
    private const int DefaultCategoryId = 5;

    public static CreateProductRequest GenerateValidCreateRequestWithoutAttachments(int? categoryId = null, string? sku = null)
        => new(
            CategoryId: categoryId ?? DefaultCategoryId,
            Name: "Test Product Name",
            SKU: sku ?? "Product SKU",
            Description: "Product Description",
            Barcode: "Product Barcode",
            SalePrice: 100,
            SupplyPrice: 90,
            RetailPrice: 95,
            QuantityInStock: 10,
            LowStockThreshold: 5,
            Packaging: new ProductPackagingDto(10, "Test Package Label", "Test Package Barcode"),
            Measurement: UnitOfMeasurement.Kilogram,
            Type: ProductType.All,
            Attachments: []);

    public static CreateProductRequest GenerateValidCreateRequestWithAttachments(int? categoryId = null, string? sku = null)
    {
        var request = GenerateValidCreateRequestWithoutAttachments(categoryId, sku);

        return request with { Attachments = GenerateAttachments() };
    }

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
            Packaging: new ProductPackagingDto(10, "Test Package Label", "Test Package Barcode"),
            Measurement: UnitOfMeasurement.Kilogram,
            Type: ProductType.All,
            Attachments: [
                CreateTestFormFile("product-1.jpg", "image/jpg"),
                CreateTestFormFile("product-2.jpg", "image/jpg")
            ]);

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
            Packaging: new ProductPackagingDto(10, "Test Package Label", "Test Package Barcode"),
            Measurement: UnitOfMeasurement.Kilogram,
            Type: ProductType.Supply,
            Attachments: [],
            ImagesToDelete: []);

    public static UpdateProductRequest GenerateValidUpdateRequestWithAttachments(int? productId, int? categoryId = null, int[]? imagesToDelete = null)
    {
        var request = GenerateValidUpdateRequest(productId, categoryId);

        return request with { Attachments = GenerateAttachments(), ImagesToDelete = imagesToDelete ?? [1, 2] };
    }

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
            Packaging: new ProductPackagingDto(10, "Test Package Label", "Test Package Barcode"),
            Measurement: UnitOfMeasurement.Kilogram,
            Type: ProductType.Sale,
            Attachments: [],
            ImagesToDelete: []);

    private static FormFile[] GenerateAttachments()
        =>
        [
            CreateTestFormFile("product-1.jpg", "image/jpeg"),
            CreateTestFormFile("product-2.jpg", "image/jpeg")
        ];

    private static FormFile CreateTestFormFile(string fileName, string contentType)
    {
        var fullPath = Path.Combine(ImageFolder, fileName);
        using var fileStream = File.OpenRead(fullPath);

        var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        return new FormFile(memoryStream, 0, memoryStream.Length, "Attachments", fileName)
        {
            Headers = new HeaderDictionary(new Dictionary<string, StringValues>
            {
                ["Content-Type"] = contentType
            }),
            ContentType = contentType
        };
    }
}
