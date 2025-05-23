using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Unit.Mappings;

public class ProductMappingsTests
{
    [Fact]
    public void ToEntity_ShouldMapAllProperties_WithValidMeasurement()
    {
        // Arrange
        var expireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var request = new CreateProductRequest(
            CategoryId: 5,
            Name: "Widget",
            SKU: "WGT-001",
            Measurement: "Kilogram",
            Description: "Heavy widget",
            Barcode: "123456",
            SalePrice: 9.99m,
            SupplyPrice: 7.50m,
            RetailPrice: 12.00m,
            QuantityInStock: 50,
            LowStockThreshold: 10,
            ExpireDate: expireDate);

        // Act
        var entity = request.ToEntity();

        // Assert
        Assert.Equal("Widget", entity.Name);
        Assert.Equal("WGT-001", entity.SKU);
        Assert.Equal("Heavy widget", entity.Description);
        Assert.Equal("123456", entity.Barcode);
        Assert.Equal(9.99m, entity.SalePrice);
        Assert.Equal(7.50m, entity.SupplyPrice);
        Assert.Equal(12.00m, entity.RetailPrice);
        Assert.Equal(50, entity.QuantityInStock);
        Assert.Equal(10, entity.LowStockThreshold);
        Assert.Equal(UnitOfMeasurement.Kilogram, entity.Measurement);
        Assert.Equal(5, entity.CategoryId);
        // Category is intentionally null after mapping
        Assert.Null(entity.Category);
    }

    [Fact]
    public void ToEntity_ShouldDefaultMeasurementToNone_WhenInvalidMeasurement()
    {
        // Arrange
        var request = new CreateProductRequest(
            CategoryId: 6,
            Name: "Gadget",
            SKU: "GDT-002",
            Measurement: "NotAUnit",
            Description: null,
            Barcode: null,
            SalePrice: 1m,
            SupplyPrice: 1m,
            RetailPrice: 1m,
            QuantityInStock: 0,
            LowStockThreshold: 0,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var entity = request.ToEntity();

        // Assert
        Assert.Equal(UnitOfMeasurement.None, entity.Measurement);
    }

    [Fact]
    public void ToCreateResponse_ShouldMapAllProperties_AndCalculateFlags()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var threshold = DateOnly.FromDateTime(now.AddDays(-7));
        var category = new Category { Id = 7, Name = "Cat7", Description = "Desc7" };
        var product = new Product
        {
            Id = 100,
            CategoryId = 7,
            Category = category,
            Name = "Prod",
            SKU = "P-100",
            Measurement = UnitOfMeasurement.Piece,
            Description = "Desc",
            Barcode = "000",
            SalePrice = 5m,
            SupplyPrice = 4m,
            RetailPrice = 6m,
            QuantityInStock = 3,
            LowStockThreshold = 5,
        };

        // Act
        var response = product.ToCreateResponse();

        // Assert
        Assert.Equal(100, response.Id);
        Assert.Equal(7, response.CategoryId);
        Assert.Equal("Cat7", response.CategoryName);
        Assert.Equal("Prod", response.Name);
        Assert.Equal("P-100", response.SKU);
        Assert.Equal(nameof(UnitOfMeasurement.Piece), response.Measurement);
        Assert.Equal("Desc", response.Description);
        Assert.Equal("000", response.Barcode);
        Assert.Equal(5m, response.SalePrice);
        Assert.Equal(4m, response.SupplyPrice);
        Assert.Equal(6m, response.RetailPrice);
        Assert.Equal(3, response.QuantityInStock);
        Assert.Equal(5, response.LowStockThreshold);
        Assert.True(response.IsLowStock);
    }

    [Fact]
    public void ToUpdateResponse_ShouldMapAllProperties_AndCalculateFlags()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var threshold = DateOnly.FromDateTime(now.AddDays(-7));
        var category = new Category { Id = 8, Name = "Cat8", Description = "Desc8" };
        var product = new Product
        {
            Id = 101,
            CategoryId = 8,
            Category = category,
            Name = "Prod2",
            SKU = "P-101",
            Measurement = UnitOfMeasurement.Piece,
            Description = "Desc2",
            Barcode = "111",
            SalePrice = 2m,
            SupplyPrice = 1m,
            RetailPrice = 3m,
            QuantityInStock = 10,
            LowStockThreshold = 5,
        };

        // Act
        var response = product.ToUpdateResponse();

        // Assert
        Assert.False(response.IsLowStock);
        Assert.Equal("Prod2", response.Name);
        Assert.Equal("P-101", response.SKU);
    }

    [Fact]
    public void ToDto_ShouldMapAllProperties_WhenCategoryIsNotNull()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var threshold = DateOnly.FromDateTime(now.AddDays(-7));
        var category = new Category { Id = 9, Name = "Cat9", Description = "Desc9" };
        var product = new Product
        {
            Id = 102,
            CategoryId = 9,
            Category = category,
            Name = "Prod3",
            SKU = "P-102",
            Measurement = UnitOfMeasurement.Box,
            Description = "Desc3",
            Barcode = "222",
            SalePrice = 7m,
            SupplyPrice = 6m,
            RetailPrice = 8m,
            QuantityInStock = 1,
            LowStockThreshold = 2,
        };

        // Act
        var dto = product.ToDto();

        // Assert
        Assert.Equal(102, dto.Id);
        Assert.Equal(9, dto.CategoryId);
        Assert.Equal("Cat9", dto.CategoryName);
        Assert.Equal("Prod3", dto.Name);
        Assert.Equal("P-102", dto.SKU);
        Assert.True(dto.IsLowStock);
    }

    [Fact]
    public void ToDto_ShouldThrowInvalidOperationException_WhenCategoryIsNull()
    {
        // Arrange
        var product = new Product
        {
            Id = 103,
            Name = "Prod4",
            SKU = "P-103",
            Category = null!
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.ToDto());
    }

    [Fact]
    public void ApplyUpdate_ShouldOverwriteAllProperties_WithValidMeasurement()
    {
        // Arrange
        var product = new Product
        {
            Name = "Old",
            SKU = "O-1",
            Measurement = UnitOfMeasurement.None,
            QuantityInStock = 1,
            LowStockThreshold = 1,
            CategoryId = 1,
            Category = new Category { Id = 1, Name = "C1", Description = "" }
        };
        var request = new UpdateProductRequest(
            Id: 1,
            CategoryId: 2,
            Name: "New",
            SKU: "N-2",
            Measurement: "Piece",
            Description: "NewDesc",
            Barcode: "999",
            SalePrice: 9m,
            SupplyPrice: 8m,
            RetailPrice: 10m,
            QuantityInStock: 20,
            LowStockThreshold: 15,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));

        // Act
        product.ApplyUpdate(request);

        // Assert
        Assert.Equal("New", product.Name);
        Assert.Equal("N-2", product.SKU);
        Assert.Equal(UnitOfMeasurement.Piece, product.Measurement);
        Assert.Equal(20, product.QuantityInStock);
        Assert.Equal(15, product.LowStockThreshold);
        Assert.Equal(2, product.CategoryId);
    }

    [Fact]
    public void ApplyUpdate_ShouldDefaultMeasurementToNone_WhenInvalidMeasurement()
    {
        // Arrange
        var product = new Product
        {
            Name = "OldX",
            SKU = "OX-1",
            Measurement = UnitOfMeasurement.Kilogram,
            CategoryId = 1,
            Category = new Category { Id = 1, Name = "C1", Description = "" }
        };
        var request = new UpdateProductRequest(
            Id: 1,
            CategoryId: 3,
            Name: "X",
            SKU: "X-1",
            Measurement: "XXX",
            Description: null,
            Barcode: null,
            SalePrice: 0m,
            SupplyPrice: 0m,
            RetailPrice: 0m,
            QuantityInStock: 0,
            LowStockThreshold: 0,
            ExpireDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        product.ApplyUpdate(request);

        // Assert
        Assert.Equal(UnitOfMeasurement.None, product.Measurement);
    }
}
